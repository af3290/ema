namespace MarketModels

module Electricity = 
    open System
    
    open MathNet    
    open MathNet.Numerics
    open MathNet.Numerics.Statistics
    open MathNet.Numerics.Interpolation
    open MathNet.Numerics.Distributions
    open MathNet.Numerics.LinearAlgebra
    open MathNet.Numerics.LinearAlgebra.Double

    open Operations
    open Types
    open MathFunctions
    open StochasticProcesses    
    open Optimization
    open Forecast
    open Simulations
    open Estimation

    (* PPA Demo, daily data *)
    
    ///Deterministic Price Function
    let gt (atT : float)
        //deterministic parameters
        (gamma1 : float) (gamma1p : float) 
        (gamma2 : float) (gamma2p : float) (gamma3 : float) : float = 
        
        //weekend indicator
        let wI = if atT % 7.0 = 5.0 || atT % 7.0 = 6.0 then 1.0 else 0.0
        
        //to unit
        let t = atT / 365.0

        //Returned value
        gamma1 * cos (2.0 * PI * t) + gamma1p * sin (2.0 * PI * t) 
      + gamma2 * cos (4.0 * PI * t) + gamma2p * sin (4.0 * PI * t) +  wI * gamma3

    ///Deterministic function estimation
    let Estimate_St (series : float[]) : float[] =
        let n = series.Length

        let optimFunc : alglib.ndimensional_func = new alglib.ndimensional_func(fun x funcRes obj -> 
            let result = Array.init n (fun t -> gt ((float)t) x.[0] x.[1] x.[2] x.[3] x.[4])
            funcRes <- RMSE (series .- result) series
        )

        let initialParams = [|0.1; 0.1; 0.1; 0.1; 0.1|]
       
        let lb = Array.init 5 (fun i -> -100.0)      
        let ub = Array.init 5 (fun i -> +100.0) 
        let bounds = array2D [|lb; ub|]

        //use linear least squares insted of multivariate...
        
        let foundParams = ConstrainedMultivariateWithBounds initialParams bounds optimFunc

        foundParams

    ///Deterministic Volatility function
    let SigSt (t : float) 
        //deterministic parameters
        (beta : float) (sigma : float) : float =
        
        //Returned value
        (0.5 + beta * cos (2.0 * PI * t) + (1.0 - beta) * sin (2.0 * PI * t + 15.0/365.0)) * sigma

    ///Deterministic function estimation
    let Estimate_SigSt (series : float[]) : float[] =
        let st = series |> Array.map (fun x -> log x)                

        let optimFunc : alglib.ndimensional_func = new alglib.ndimensional_func(fun x funcRes obj -> 
            let result = series |> Array.mapi (fun t st -> SigSt ((float)t/365.0) x.[0] x.[1])
            funcRes <- RMSE (series .- result) series
        )

        let initialParams = [|0.1; 0.1;|]
        
        let lb = Array.init 5 (fun i -> -100.0)      
        let ub = Array.init 5 (fun i -> +100.0) 
        let bounds = array2D [|lb; ub|]

        let foundParams = ConstrainedMultivariateWithBounds initialParams bounds optimFunc

        foundParams
       
    type DeterministicSeasonality = {
        gamma1 : float; gamma1p : float;
        gamma2 : float; gamma2p : float; gamma3 : float;
    }

    type DeterministicVolatility = {
        beta : float; sigma : float;
    } 

    type PowerPurchasingAgreementModelParams = {
        DeterministicSeasonality : DeterministicSeasonality;
        DeterministicVolatility : DeterministicVolatility;
        MeanReversion: OU;
    }

    type PowerPurchasingAgreement (margin : float, interestRate : float, retailPrice : float) = 
        //input members
        let mutable InterestRate = interestRate
        let mutable Margin = margin
        let mutable RetailPrice = retailPrice
        //local members        
        let mutable ouParams = {mu = 0.1; lambda = 0.2; sigma = 0.3}
        let mutable seasParams = [|1.0; 0.2; 0.5;0.2; 0.5|]
        let mutable sigParams = [|0.1; 0.1;|]

        let mutable (Sts:float[,]) = null
        let mutable (DPs:float[,]) = null

        //histogram...
        let mutable (binLength:float) = 0.0
        let mutable (binsValues:float[]) = null
        let mutable (hist:Histogram) = null
        
        ///nbPaths by horizon in days
        member this.SpotPrices with get() = Array.init (Sts.GetLength(0)) (fun i -> Sts.[i, *])

        member this.Histogram with get() = hist

        member this.ModelParameters with get() = {
                                                    DeterministicSeasonality = {
                                                                                gamma1 = seasParams.[0];
                                                                                gamma1p = seasParams.[1];
                                                                                gamma2 = seasParams.[2]; 
                                                                                gamma2p = seasParams.[3];
                                                                                gamma3 = seasParams.[3];
                                                                            };
                                                    DeterministicVolatility = {
                                                                                beta = sigParams.[0];
                                                                                sigma = sigParams.[1];
                                                                            };
                                                    MeanReversion = ouParams
                                                }

        member val Value = 0.0 with get, set

        ///Option value as defined
        member this.DailyValue (spotPrice : float) (margin : float) (contractPrice : float) : float = 
            max (spotPrice - margin) contractPrice

        ///Payoff function
        member this.DailyPayoff (spotPrice : float) (contractPrice : float) (quantity : float) : float = 
            (spotPrice - contractPrice) * quantity

        ///Profit function
        member this.DailyProfit (load : float) (quantity : float) (spotPrice : float) (contractPrice : float) =
             load * 50.0 + (quantity - load) * spotPrice - 0.5 * quantity * contractPrice

        ///Returns the daily production based on wind speed in MW
        member this.DailyProduction (windSpeed : float) : float =
            if 5.0 <= windSpeed && windSpeed <= 15.0 then 240.0 * (windSpeed - 5.0) else
            if 15.0 < windSpeed && windSpeed < 25.0 then 2400.0 else
            0.0

        ///Simulates autoregressive production...
        member this.SimulateProduction (horizon : int) (nbPaths : int) : float[,] =
            let rndMat = DenseMatrix.CreateRandom(nbPaths, horizon, new Beta(1.6, 5.0)); 
            let rndPaths = rndMat.ToRowArrays()

            let a = 0.4

            let genPath (dWs:float[]) = dWs |> Array.scan(fun prevVal w -> a * prevVal + (1.0 - a) * 45.0 * w) 0.0
            
            let toProduction = fun x -> this.DailyProduction x

            let simulations = rndPaths |> Array.map (fun rndPath -> genPath rndPath |> Array.map toProduction)

            array2D simulations

        ///Horizon in days...
        member this.SimulateSpotPrices (S0 : float) (horizon : int) (nbPaths : int) : float[,] =
            //do antithetic price simulations
            let rndMat = DenseMatrix.CreateRandom(nbPaths/2, horizon, new Normal()).ToArray();
            let rndMatNeg = rndMat |> Array2D.map (fun x -> -x)
            
            //simulate wind values
            //SpecialFunctions.Beta()

            //put all randoms numbers together
            let rnds = concat2D rndMat rndMatNeg true
            let rndPaths = DenseMatrix.OfArray(rnds).ToRowArrays()
            
            //add deterministiv volatility
            let sigmas = Array.init horizon (fun t -> (SigSt ((float)t) sigParams.[0] sigParams.[1]) + ouParams.sigma)
            let dps = seasParams
            let seasonal t = gt ((float)t) dps.[0] dps.[1] dps.[2] dps.[3] dps.[4]
            //this is IMPORTANT!!!, to re write to kappa for exp transform...!!!
            let kappa = 1.0 / ouParams.lambda
            let genPath (dWs:float[]) = generateMeanRevertingPathWithSigmas S0 dWs (1.0/365.0) kappa ouParams.mu sigmas 
            //add deterministic seasonal
            let genPathDet (dWs:float[]) = (genPath dWs) |> Array.mapi (fun t x -> exp (x + seasonal t))    
            let simulations = rndPaths |> Array.map (fun rndPath -> genPathDet rndPath)

            array2D simulations

        /// Evaluates and assign the value of the PPA agreement
        member this.Evaluate (seriesX : float[]) (horizon : int) (confidence : float) = 
            //together or separately...?
            let n = seriesX.Length

            let series = seriesX |> Array.map (fun x -> log x)    

            //estimate OU
            ouParams <- OU_MLE series 0.25

            //estimate deterministic price, it makes too much of an impact to do it... really?...
            let dps = [|0.062; 0.013; 0.009; 0.012; -0.031|] //Estimate_St series
            seasParams <- dps
            let residuals = series .- Array.init n (fun t -> gt ((float)t) dps.[0] dps.[1] dps.[2] dps.[3] dps.[4])

            //estimate deterministic volatility
            sigParams <- Estimate_SigSt residuals

            //find minimum nb of simulations for the required confidence
            let nbSims = 100 * MinimumSimulationsCountForNormalConfidenceOf 0.5 0.3 confidence
                    
            //what do we actually do with the log shit?
            let S0 = series.[series.Length - 1]
            
            //Simulate stuff
            let days = (365*horizon)
            DPs <- this.SimulateProduction days nbSims
            Sts <- this.SimulateSpotPrices S0 days nbSims
            
            let pathValuesAtTs = DPs |> Array2D.mapi (fun i t x  -> exp(-(float)t*1./365.) * 0.5 * DPs.[i, t] * (Sts.[i, t] - Margin))
            let pathsValue = pathValuesAtTs |> sum2D 
            
            //Assign Value
            this.Value <- pathsValue * 1.0 / (float)days

            //Compute Distribution
            let pathsValues = Array.init nbSims (fun i -> pathValuesAtTs.[i, *] |> sum)
            let nbBins = 25
            hist <- new Histogram(pathsValues, nbBins)
            binLength <- (hist.UpperBound - hist.LowerBound)/(float)nbBins
            binsValues <- Array.init nbBins (fun i -> hist.[i].Count / (float)nbBins)

        member this.EvaluateAsOption (seriesX : float[]) (horizon : int) (confidence : float) = 
            seriesX

        
    ///Holds a Mean-Reversion with Jumps model with a maximum 1 jump per day, matlab demo...
    ///etc...
    type MeanReversionWithJumpsDemo(X : float[], zzz : float) =
        let Pt = after X 1
        let Pt_1 = before X (X.Length - 1)
        let dt = 1.0 / 365.0
        
        [<DefaultValue>] val mutable alpha : float
        [<DefaultValue>] val mutable kappa : float
        [<DefaultValue>] val mutable mu_J : float
        [<DefaultValue>] val mutable sigma : float
        [<DefaultValue>] val mutable sigma_J : float
        [<DefaultValue>] val mutable lambda : float

        //just KK
        [<DefaultValue>] val mutable allParams : float[]

        ///keeps matlab order
        member this.MLEEstimationPDF  
            (a : float) 
            //Jumps Parameters
            (phi : float) (mu_J : float)  (sigmaSq : float) 
            //Mean-Reversion Parameter
            (sigmaSq_J : float) (lambda : float) : float[] =
                        
            let pdf pt pt_1 =
                //Jumps Component 
                let jumpSig = 2.0 * (sigmaSq+sigmaSq_J)
                let jump = -((pt - a - phi * pt_1 - mu_J) ** 2.0) / jumpSig                
                let jumpTerm = (exp jump) * (1.0 / sqrt(jumpSig * PI))
                                
                //Mean Reversion Component
                let meanReversionSig = 2.0 * sigmaSq
                let meanReversion = -((pt - a - phi * pt_1) ** 2.0) / meanReversionSig
                let meanReversionTerm = (exp meanReversion) * (1.0 / sqrt(meanReversionSig * PI))                                

                lambda * jumpTerm + (1.0 - lambda) * meanReversionTerm

            //notice that it relies on the type parameter
            let result = Array.map2 pdf Pt Pt_1 
            
            result

        member this.EstimateModel() =
            let x0 = [|0.0; 0.0; 0.0; var X; var X; 0.5|];

            let lb = [|-Inf; -Inf; -Inf; 0.001; 0.001; 0.001|];
            let ub = [|Inf; 1.0; Inf; Inf; Inf; 1.0|];

            let pdfFunc (x : float[]) = this.MLEEstimationPDF x.[0] x.[1] x.[2] x.[3] x.[4] x.[5]

            let optimized = MLE Pt x0 lb ub pdfFunc

            this.allParams <- optimized

            //model parameters
            this.alpha <- optimized.[0]/dt
            this.kappa <- optimized.[1]/dt
            this.mu_J <- optimized.[2]
            this.sigma <- sqrt(optimized.[3]/dt)
            this.sigma_J <- sqrt(optimized.[4])
            this.lambda <- optimized.[5]/dt           

        member this.Simulate() =
            ()