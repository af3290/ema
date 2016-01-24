namespace MarketModels

module TimeSeries =
    //TODO: port from matlab... yes, awesome...
    open System
    open System.Collections.Generic    
    open Operations
    open Optimization
    open Forecast
    open MathNet.Numerics
    open MathNet.Numerics.Statistics
    open MathNet.Numerics.LinearAlgebra.Double
    open MathFunctions

    ///Convention: missing lags are fixed at 0, not missing with 0s are to be optimized...
    type LagOp = {
        Coefficients : float[];
        Lags : int[];
        
    } with
        member this.Degree with get() = this.Lags |> Seq.last

    type ARMAResult = {
        //always corresponding to as nbOfLags in increasing order
        AR : float[]; 
        MA : float[]; 
        Const : float; 
        Var: float
    }

    ///Simple univariate ARMA estimation based on internal.econ.arma0, (as similar as possible to its code) from
    ///matlab. Returns the coefficients for ARMA, its constant and variance
    //TODO: refactor when done...
    let ARMA (series : float[]) (ar : LagOp) (ma : LagOp) : ARMAResult =
        let p = ar.Degree
        let q = ma.Degree

        if p < 0 || q < 0 then
            failwith "Invalid ARMA parameters"

        //parameters of the result to be determined
        let mutable AR = Array.init p (fun i -> 0.0)
        let mutable MA = Array.init q (fun i -> 0.0)
        let mutable Const = 0.0
        let mutable Var = 0.0

        //local calculation variables
        let mutable C = Array2D.init p p (fun i j -> 0.0)
        let mutable d = Array.init p (fun i -> 0.0)
        
        if p > 0 then 
            let autocor = SeriesAutocorrelationFFT series (p + q)            
            let var = Statistics.Variance(series)
            let covar = autocor |> Array.map (fun x -> x * var)
            
            //don't deal with non finte case... since we're starting from black, never from already passed coefficients/state

            //teoplitz matrix...
            if q > 0 then
                let row = { q.. -1 .. q - p + 1 } |> Seq.map abs |> Seq.map (fun x -> covar.[x]) |> Seq.toArray
                C <- TeoplitzInit covar.[q..q+p-1] row
                d <- covar.[q+1..q+p]                
            else
                C <- TeoplitzInit covar.[0..p-1] covar.[0..p-1]
                d <- covar.[0..p]                
            
            let mutable rmatrixsolveInt = 0

            //has no 0 fixed lags
            if ar.Degree = ar.Lags.Length then
                //find AR coefficients using solving a linear system,                
                let mutable rmatrixsolveRep : alglib.densesolverreport = null
                //solve linear system
                alglib.rmatrixsolve(C, C.GetLength(0), d, &rmatrixsolveInt, &rmatrixsolveRep, &AR)
            //has some 0 fixed lags
            else
                //find AR coefficients using linear least squares...
                //find missing lags fixed to 0, adjust to 0 index...
                let fixedARsLags = Set.ofArray(Array.init (ar.Degree) (fun i -> i + 1)) - Set.ofArray(ar.Lags) |> Set.toArray               
                //setup constraints
                let beq = Array.init fixedARsLags.Length (fun i -> 0.0)
                //adjust for 0 starting index...
                let Aeq = Array2D.init fixedARsLags.Length ar.Degree (fun i j -> if j = (fixedARsLags.[i]-1) then 1.0 else 0.0)
                //run solver
                AR <- ConstrainedLinearLeastSquares C d Aeq beq

            //where is polynomialsolve ...????
            //no need for stationarity test, since no state...
        
        let bCoeffs = Array.init (AR.Length + 1) (fun i -> if i = 0 then 1.0 else -AR.[i-1])        
        let filteredSeries = Filter1D bCoeffs [|1.0|] series   
        
        //variance of the filtered series now...     
        Const <- filteredSeries |> Array.average
        let var = Statistics.Variance(filteredSeries)

        //we're done with it, return
        if q = 0 then 
            {
                AR = AR;
                MA = MA;
                Const = Const;
                Var = var
            }
        else
            //continue estimating MAs
            let autocor = SeriesAutocorrelationFFT filteredSeries q
            let c = autocor |> Array.map (fun x -> x * var) //add 1 back...???
        
            //start MA optimization loop
            let tol = 0.01
            let mutable counter = 0
        
            let MA1 = Array.init q (fun i -> 1.0)
            let fixedMAsLags = Set.ofArray(Array.init (ma.Degree) (fun i -> i + 1)) - Set.ofArray(ma.Lags) |> Set.toArray               
              
            //MA minimize change variance until a significant fit is found
            while (L2Norm (MA -- MA1) > tol && counter < 100) do
                //put data in previous state to check norm change...
                MA |> Array.iteri (fun i x -> MA1.[i] <- x)

                //variance of innovation process e(t)
                let variance = c.[0] / (1.0 + Array.sum (MA .* MA))

                //eigenvalues invertibility... no now

                //moving average coefficients estimation
                for j in q - 1 .. -1 .. 0 do
                    //not a fixed lag
                    if Array.IndexOf(fixedMAsLags, (j+1)) = - 1 then
                        //no state, so no need to keep it, check it thoroughly... revise
                        let maPart = if q = 1 then 0.0 else MA.[0..q-j-1] .* MA.[j..q-1] |> Array.sum
                        //subsequent autocorrelations for each 
                        MA.[j] <- c.[j+1] * 1.0 / variance - maPart

                counter <- counter + 1
            
            //eliminate all fixed 0 lags from MA
            MA <- Array.init ma.Lags.Length (fun i -> MA.[ma.Lags.[i]-1])
                                             
            {
                AR = AR;
                MA = MA;
                Const = Const;
                Var = Var
            }

    ///Shortcut method to avoid using LagOps, initializes LagOps with all 1..p and q...
    let ARMASimple (series : float[]) (p : int) (q : int) : ARMAResult = 
        let ar = {
            Coefficients = Array.init p (fun i -> 0.0);
            Lags = Array.init p (fun i -> i)
        }
        
        let ma = {
            Coefficients = Array.init q (fun i -> 0.0);
            Lags = Array.init q (fun i -> i)
        }

        ARMA series ar ma

    ///Shortcut method to avoid using LagOps, pass just the lags to be optimized...
    let ARMASimple2 (series : float[]) (p : int[]) (q : int[]) : ARMAResult = 
        let ar = {
            Coefficients = Array.init p.Length (fun i -> 0.0);
            Lags = p
        }
        
        let ma = {
            Coefficients = Array.init q.Length (fun i -> 0.0);
            Lags = q
        }

        ARMA series ar ma

    ///Simple forecasting of an ARMA fit
    let Forecast (series : float[]) (arma : ARMAResult) (forecastSteps : int): ForecastResult = 
        ///etc...
        {
            Forecast = [||];
            //each pairs level, f.ex. 95%
            ConfidenceLevels = [||];
            //from highest to lowest prediction intervals
            Confidence = array2D [|[||]|] //because the inners have the same lengths! not array of arrays!
        }

