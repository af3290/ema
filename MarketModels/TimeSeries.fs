namespace MarketModels

///Hold common object for time series problmes
module TimeSeries =
    //TODO: port from matlab... yes, awesome...
    open System
    open System.Collections.Generic        
    open MathNet.Numerics
    open MathNet.Numerics.Statistics
    open MathNet.Numerics.LinearAlgebra
    open MathNet.Numerics.LinearAlgebra.Double
    open MathNet.Numerics.LinearAlgebra.Matrix
    open MathNet.Numerics.LinearAlgebra.MatrixExtensions
    open MathNet.Numerics.LinearAlgebra.DenseMatrix
    
    open Operations
    open MathFunctions    
    open StochasticProcesses
    open Optimization
    open Forecast

    let toStationary (series : float[]) : float[] =
        let Xs = Array.init series.Length (fun i -> (float)i)
        let ab = Fit.Line(Xs, series)
        let a = fst ab
        let b = snd ab
        let trend = Xs |> Array.map (fun x -> a*x + b)
        let ymean = mean series
        let detrendedSeries = Array.map2 (fun y ytrend -> y - ytrend + ymean) series trend
        series

    ///Add presamples data, and XXX... Helper method, from matlab.. uses column vectors...
    let checkPresampleData(outputMatrix : float[,]) (intputMat : float[,]) (nRows : int) : float[,] =
        let rows = (outputMatrix.GetLength 0)
        let columns = (outputMatrix.GetLength 1)

        //TODO: add checks...        

        //Reformat presample
        let localInputMat = lastRows2D intputMat nRows
        
        //If the user-specified input presample array is a single column vector, replicate it across all columns 
        if (localInputMat.GetLength 1) = 1 then
            for i in 0..columns - 1 do
                outputMatrix.[(rows-nRows)..(rows-1), i] <- localInputMat.[*, 0]
        else
            //assume same number of columns...
            for i in 0..columns - 1 do
                outputMatrix.[(rows-nRows)..(rows-1), i] <- localInputMat.[*, i]

        outputMatrix


    ///Convention: missing lags are fixed at 0, not missing with 0s are to be optimized...
    type LagOp = {
        Coefficients : float[];
        Lags : int[];        
    } with
        member this.Degree with get() = if Array.isEmpty this.Lags then 0 else this.Lags |> Seq.last

    type ARMAResult = {
        //always corresponding to as nbOfLags in increasing order, excluding 0 self AR
        AR : LagOp; 
        MA : LagOp; 
        Const : float; 
        Var: float;
        [<DefaultValue>] mutable Rand: int -> int -> float[][]
        //TODO: merge with ARXMA or ARMAX model..
    } with 
        ///Evaluates the model on a time series by calculating next step values/residuals, alike to matlabs parts.
        ///Returns for first maxDegree 0s
        member this.EvaluateWithResidualsAndLagOps (Y : float[]) (E : float[]) (ar : LagOp) (ma : LagOp) (isOnResiduals : bool) : float[] = 

            let maxDegree = max ar.Degree ma.Degree

            if Y.Length < maxDegree then
                failwith "Not enough observations"
                        
            let E = if(E.Length = 0) then Array.zeroCreate Y.Length else E
                        
            if Y.Length <> E.Length then
                failwith "When passing residuals, lengths must agree"

            //when calculating residuals must subtract the constant
            let coeffsSign = if isOnResiduals then -1.0 else 1.0

            //on residuals must include self 0 AR
            let arLags = if isOnResiduals then Array.concat [| [|0|]; ar.Lags|] else ar.Lags
            let arCoeffs = if isOnResiduals then  Array.concat [| [|-1.0|]; ar.Coefficients|] else ar.Coefficients

            let coeffs = Array.concat [|[|this.Const * coeffsSign|]; arCoeffs .*. coeffsSign; ma.Coefficients .*. coeffsSign|]
        
            for i in maxDegree..Y.Length-1 do
                //revert indices from lag form to array indices, for example [|1; 2; 24|] to [|23; 22; 0|]
                let arIdx = Array.rev ((Array.rev arLags) |> Array.map (fun x -> - x + i))
                let maIdx = Array.rev ((Array.rev ma.Lags) |> Array.map (fun x -> - x + i))
            
                //get the values at the specified indices
                let data = Array.concat [| [|1.0|]; sub Y arIdx; sub E maIdx |]

                //arrays sum product
                let nextValue = data .* coeffs |> sum

                if isOnResiduals then 
                    E.[i] <- nextValue     
                else
                    Y.[i] <- nextValue

            if isOnResiduals then 
                E
            else
                Y
        
        ///Another shorthand...
        member this.EvaluateWithResiduals (Y : float[]) (E : float[]) (isOnResiduals : bool) : float[] = 
            this.EvaluateWithResidualsAndLagOps Y E this.AR this.MA isOnResiduals

        ///Shorthand method...
        member this.Evaluate (Y : float[]) (isOnResiduals : bool) : float[] = 
            this.EvaluateWithResiduals Y [||] isOnResiduals

        ///Simple simulation routine of an ARMA fit...
        ///simulations - numPaths
        member this.Simulate (simulations : int) (horizon : int) (Y0 : float[]) (E0 : float[]) : float[,] =         
            
            let maxDegree = max this.AR.Degree this.MA.Degree
            let T = horizon + maxDegree

            //presample E0
            let inE0 = array2D (JaggedArray.transpose [|E0|])
            let e0 = checkPresampleData (zeros maxDegree simulations) inE0 this.MA.Degree
            //mmmh???
            let E = Array2D.init simulations T (fun i j -> if j < maxDegree then e0.[j, 0] else 0.0)
            
            //presample Y0
            let inY0 = array2D (JaggedArray.transpose [|Y0|])
            let y0 = checkPresampleData (ones maxDegree simulations) inY0 this.AR.Degree
            //mmmh???
            let Y = Array2D.init simulations T (fun i j -> if j < maxDegree then y0.[j, 0] else 0.0)

            let x = new Double.DenseMatrix(simulations, horizon)
            
            let randoms = this.Rand simulations horizon
            
            //add 0 MA lag...???
            let ma = {
                Lags = Array.concat [|[|0|]; this.MA.Lags|]
                Coefficients = Array.concat [|[|1.0|]; this.MA.Coefficients|]
            }

            let simRes = Array.init simulations (fun i -> 
                //exclude the presample estimation and scale to process variance
                let randomPath = Array.concat [| E.[i, 0..maxDegree-1]; randoms.[i] .*. sqrt this.Var |]
                
                //evaluate path on itself
                let simulatedPath = this.EvaluateWithResidualsAndLagOps Y.[i,*] randomPath this.AR ma false

                //return after presample
                after simulatedPath maxDegree
            )

            //Consider returning the errors as well... 
            array2D simRes

        ///Caluates gaussian NLogLikelihood
        member this.LogLikelihood (Y : float[]) : float =
            //in TODO:
            0.0

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
        let filteredSeries = Filter1D1 bCoeffs series   

        //variance of the filtered series now...     
        Const <- filteredSeries |> Array.average
        let var = Statistics.Variance(filteredSeries)

        //we're done with it, return
        if q = 0 then
            //update lagOps coeffs
            AR |> Array.iteri (fun i x -> ar.Coefficients.[i] <- x) 
            MA |> Array.iteri (fun i x -> ma.Coefficients.[i] <- x) 

            {
                AR = ar;
                MA = ma;
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
            while (L2Norm (MA .- MA1) > tol && counter < 100) do
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
            
            //eliminate all fixed 0 lags from the long AR and MA arrays
            AR <- Array.init ar.Lags.Length (fun i -> AR.[ar.Lags.[i]-1])
            MA <- Array.init ma.Lags.Length (fun i -> MA.[ma.Lags.[i]-1])
            
            //update ar and ma lagOps coeffs from locals, TODO: revise...
            AR |> Array.iteri (fun i x -> ar.Coefficients.[i] <- x) 
            MA |> Array.iteri (fun i x -> ma.Coefficients.[i] <- x) 
                                                         
            {
                AR = ar;
                MA = ma;
                Const = Const;
                Var = var
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

        let res = ARMA series ar ma
        res.Rand <- StochasticProcesses.getStandardNormalSampleMatrixSimple
        res

    let ARMASimple3 (ar : LagOp) (ma : LagOp) (constant : float) (variance : float) : ARMAResult = 
        let res = {
            AR = ar;
            MA = ma;
            Const = constant;
            Var = variance
        }
        res.Rand <- StochasticProcesses.getStandardNormalSampleMatrixSimple
        res

    //TODO: switch to object oriented...? maybe...
    

    ///Simple forecasting of an ARMA fit
    let Forecast (series : float[]) (residuals : float[]) (arma : ARMAResult) (horizon : int) (alpha : float) : ForecastResult = 
        
        let maxDegree = max arma.AR.Degree arma.MA.Degree

        let T = horizon + maxDegree
        
        //from checkPresampleData... take last maxDegree observations
        let presample = Array.sub (Array.rev series) 0 maxDegree |> Array.rev       
        let Y = Array.concat [|presample; Array.zeroCreate horizon|]
                       
        let y = arma.Evaluate Y false     

        //confidence alpha...
        let alphaBounds = ConfidenceAlphaBounds alpha
        let cis = [|fst alphaBounds; alpha; snd alphaBounds|]

        //we have several methods of calculating PIs... from matlab... by seasonal folding...
        //we re not coming from infer
        let backcast = if series.Length = residuals.Length then series .+ residuals else [||]
        let confidence = 
            if series.Length = residuals.Length then 
                    let inAndOutSampleSeries = Array.concat [|series; y.[maxDegree..T-1]|]
                    NormalPredictionIntervalsFromSeries inAndOutSampleSeries residuals horizon cis 
                else 
                    array2D [||]

        {
            Backcast = backcast
            Forecast = y.[maxDegree..T-1];
            //each pairs level, f.ex. 95%
            ConfidenceLevels = cis;
            //from highest to lowest prediction intervals
            Confidence = confidence
        }

    ///Calculation of residuals of an ARMA model fit ...
    let Infer (arma : ARMAResult) (series : float[]) : float[] =
        
        let maxDegree = max arma.AR.Degree arma.MA.Degree

        let T = series.Length + maxDegree

        let E0 = Array.zeroCreate maxDegree
        
        //revert series and forecast backwards
        let res = Forecast (Array.rev series) E0 arma maxDegree 0.95

        //equivalent of backcasting to initialize observation in presample, from 0 to maxDegree
        let Y0 = (Array.rev res.Forecast)

        let Y = Array.concat [| Y0; series |]

        let E = arma.Evaluate Y true

        //eliminate presample, which was just used for initialization
        Array.sub E maxDegree series.Length

    type ARXModel = {
        AR : LagOp;
        //Betas of the predictors data given by X
        Beta : float[];
        Residuals : float[];
        //Regression variance and constant
        Var : float;
        Const : float;
    }

    ///estimates AR with exogenous variables... estimate.m:1522 arx0(...)
    ///Y - nbObservations 
    ///X - nbObservations by nbPredictors
    let ARX (Y : float[]) (X : float[,]) (ar : LagOp) : ARXModel =
        if Y.Length <> X.GetLength(0) then
            failwith "Input dimensions don't agree"

        let maxDegree = ar.Degree
        
        //add lagged columns... NOT DONE PROPERLY...
        let xOnes = Array2D.init Y.Length 1 (fun i j -> 1.0)
        let yLagged = LaggedMatrix Y ar.Lags
        let xOnesYLagged = concat2D xOnes yLagged false
        let xWithYLagged = concat2D xOnesYLagged X false

        //eliminate presample needed for AR initialization
        let x = afterRows2D xWithYLagged maxDegree
        let xMat = DenseMatrix.OfArray x
        
        let y = after Y maxDegree
        let yMat = DenseMatrix.OfColumnVectors(DenseVector.OfArray(y))
        
        let qr = xMat.QR()
        let q = qr.Q.Transpose().Multiply(yMat)
                
        let C = qr.R.ToArray()
        let d = q.ToColumnArrays().[0]
        
        //empty constraints
        let Aeq = Array2D.init 1 (C.GetLength(0)) (fun i j -> 0.0)
        let beq = [|0.0|]

        let bbb = ConstrainedLinearLeastSquares C d Aeq beq
        
        //already exclude the initial maxDegree presample
        let residuals = y .- xMat.Multiply(DenseVector.OfArray(bbb)).ToArray()

        //assign output betas
        //yet SAR are excluded... 
        let bConst = bbb.[0]
        let b = after bbb (1 + ar.Lags.Length)   
        let var = Statistics.Variance(residuals)
        bbb.[1..ar.Lags.Length] |> Array.iteri (fun i x -> ar.Coefficients.[i] <- x)
        
        {
            AR = ar;
            Beta = b;
            Residuals = residuals; //we don't really need the reisualds, since they can be easily calculated...
            Const = bConst;
            Var = var
        }

    type ARXMAModel = {
        //The contained ARX model
        ARX : ARXModel;
        //The moving average part
        MA : LagOp;
    } with
        ///Evaluates the model on a time series by calculating next step values/residuals, alike to matlabs parts.
        ///Returns for first maxDegree 0s
        member this.Evaluate (Y : float[]) (X : float[,]) (isOnResiduals : bool) : float[] = 

            if Y.Length < X.GetLength(0) then
                failwith "Dimensions must agree"

            let maxDegree = max this.ARX.AR.Degree this.MA.Degree
            
            if Y.Length < maxDegree then
                failwith "Not enough observations"
            
            let E = Array.zeroCreate Y.Length
                        
            //when calculating residuals must subtract the constant
            let coeffsSign = if isOnResiduals then -1.0 else 1.0

            //on residuals must include self 0 AR
            let arLags = if isOnResiduals then Array.concat [| [|0|]; this.ARX.AR.Lags|] else this.ARX.AR.Lags
            let arCoeffs = if isOnResiduals then  Array.concat [| [|-1.0|]; this.ARX.AR.Coefficients|] else this.ARX.AR.Coefficients

            let coeffs = Array.concat [|[|this.ARX.Const * coeffsSign|]; arCoeffs .*. coeffsSign; this.MA.Coefficients .*. coeffsSign|]
        
            for i in maxDegree..Y.Length-1 do
                //revert indices from lag form to array indices, for example [|1; 2; 24|] to [|23; 22; 0|]
                let arIdx = Array.rev ((Array.rev arLags) |> Array.map (fun x -> - x + i))
                let maIdx = Array.rev ((Array.rev this.MA.Lags) |> Array.map (fun x -> - x + i))
            
                //get the values at the specified indices
                let data = Array.concat [| [|1.0|]; sub Y arIdx; sub E maIdx |]
                
                //predictors regression term
                let predVal = this.ARX.Beta .* X.[i, *] |> sum
                
                //series ARMA term
                let nextValue = data .* coeffs |> sum
                
                //total, adjust prediction sign based on whether we're dealing with reasiduals or not
                let nextStepValue = coeffsSign * predVal + nextValue

                if isOnResiduals then 
                    E.[i] <- nextValue     
                else
                    Y.[i] <- nextValue

            if isOnResiduals then 
                E
            else
                Y
        //Calculation of forecast for given data and params
        member this.Forecast(series : float[]) (residuals : float[]) (X : float[,]) (horizon : int) (alpha : float) : ForecastResult =         
            if series.Length + horizon <> X.GetLength(0) then
                failwith "X must contain the in-sample predictors and the forecasted X values for the horizon (X0 and XF)"

            let maxDegree = max this.ARX.AR.Degree this.MA.Degree

            if horizon > maxDegree then
                failwith "Can't forecast more than the maximum ARMA degree"

            let T = horizon + maxDegree
        
            //from checkPresampleData... take last maxDegree observations
            let presampleY = after series maxDegree 
            let inY = Array.concat [|presampleY; Array.zeroCreate horizon|]
            let inX = lastRows2D X inY.Length
            let y = this.Evaluate inY inX false     

            //confidence alpha...
            let alphaBounds = ConfidenceAlphaBounds alpha
            let cis = [|fst alphaBounds; alpha; snd alphaBounds|]

            //we have several methods of calculating PIs... from matlab... by seasonal folding...
            //we re not coming from infer
            let backcast = if series.Length = residuals.Length then series .+ residuals else [||]
            let confidence = 
                if series.Length = residuals.Length then 
                        let inAndOutSampleSeries = Array.concat [|series; y.[maxDegree..T-1]|]
                        NormalPredictionIntervalsFromSeries inAndOutSampleSeries residuals horizon cis 
                    else 
                        array2D [||]

            {
                Backcast = backcast
                Forecast = y.[maxDegree..T-1];
                //each pairs level, f.ex. 95%
                ConfidenceLevels = cis;
                //from highest to lowest prediction intervals
                Confidence = confidence
            }
        ///Calculation of residuals of an ARXMA model fit ... series and X must have same length!
        member this.Infer (series : float[]) (X : float[,])  : float[] =    
                
            let maxDegree = max this.ARX.AR.Degree this.MA.Degree
            
            let E0 = Array.zeroCreate maxDegree
            
            //revert series and predictors to forecast backwards for the intialization presample period
            let fY = Array.rev series
            let fX1 = rev2D X true
            
            //assume same predictors values for presample, is that good... no!
            let fX0 = fX1.[0..maxDegree-1,*]
            let fX = concat2D fX0 fX1 true

            //presample... practically from 0 to -maxDegree, for backcasting
            let Y0 = (Array.rev ((this.Forecast fY E0 fX maxDegree 0.95).Forecast))
            
            //then couple the presample and get all residuals
            let Y = Array.concat [| Y0; series |]
            
            let E = this.Evaluate Y fX true
            
            after E maxDegree

        member this.Simulate (series : float[]) (X : float[,])  : float[] =    

            series

    ///estimates ARXMA model with exogenous variables calling the previous ARX model
    ///Y - nbObservations 
    ///X - nbObservations by nbPredictors
    let ARXMA (Y : float[]) (X : float[,]) (ar : LagOp) (ma : LagOp) : ARXMAModel =
        if Y.Length <> X.GetLength(0) then
            failwith "Input dimensions don't agree"

        let arx = ARX Y X ar

        //use residuals for calculation of MA part
        let y = after arx.Residuals ar.Degree

        //use an empty AR to get only the MA part estimated
        let ear = {
            Coefficients = [||];
            Lags = [||]
        }

        let emptyAR = ARMA y ear ma
                
        //const and var should be the same now...

        {
            ARX = arx;
            MA = emptyAR.MA;            
        }

    ///Shortcut method to avoid using LagOps, pass just the lags to be optimized...
    let ARXMASimple2 (Y : float[]) (X : float[,]) (p : int[]) (q : int[]) : ARXMAModel =
        let ar = {
            Coefficients = Array.init p.Length (fun i -> 0.0);
            Lags = p
        }
        
        let ma = {
            Coefficients = Array.init q.Length (fun i -> 0.0);
            Lags = q
        }

        ARXMA Y X ar ma

    ///Calculates model diagnostics... yes... using matlab method... etc...
