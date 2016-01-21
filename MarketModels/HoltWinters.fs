namespace MarketModels

module HoltWinters =
    open System
    open Forecast
    
    ///Triple seasonal Holt Winters method, additive version (for multiplicative use use natural logarithm)
    ///Returns original data and the forecasted values appended
    let TripleHWT (data : float[]) (seasonLength : int) (nbForecastSteps :int) (alpha : float) (beta : float) (gamma : float) : float[] =
        let seasons = data.Length / seasonLength

        if seasons < 1 then
            failwith "Not enough data" 

        let firstPart = data.[0..seasonLength]
        let secondPart = data.[seasonLength..2*seasonLength]
        let newLength = data.Length + nbForecastSteps

        let Y = Array.init newLength (fun i -> if i < data.Length then data.[i] else 0.0)

        //first values initialization
        let a = Array.init newLength (fun x -> 0.0)
        a.[0] <- Array.sum firstPart / (float)seasonLength

        let b = Array.init newLength (fun x -> 0.0)
        b.[0] <- (Array.sum secondPart - Array.sum firstPart) / ((float)seasonLength ** 2.0)

        let s = Array.init newLength (fun i -> if i <= seasonLength then data.[i]-a.[0] else 0.0)

        let y = Array.init newLength (fun x -> 0.0)
        y.[0] <- a.[0] + b.[0] + s.[0]

        //subsequent calculations
        for i in 0..data.Length + nbForecastSteps - 2 do
            if i > data.Length - 1 then
                Y.[i] <- a.[i] + b.[i] + s.[i - nbForecastSteps] //??

            //TODO: replace i with i+1... etc...
            a.[i+1] <- alpha * (Y.[i] - s.[i]) + (1.0 - alpha) * (a.[i] + b.[i])
            b.[i+1] <- beta * (a.[i + 1] - a.[i]) + (1.0 - beta) * b.[i]
            s.[i+1] <- gamma * (Y.[i] - a.[i] - b.[i]) + (1.0 - gamma) * s.[i]
            y.[i+1] <- a.[i + 1] + b.[i + 1] + s.[i + 1]
        
        y

    type HoltWintersParams = {
        alpha : float;
        beta : float;
        gamma : float;
    }

    //TODO: fix, doesn't find the right parameters yet... but it will...
    ///finds the best paramters for HWT and returns them
    let OptimizeTripleHWT (data : float[]) (seasonLength : int) (nbForecastSteps :int) : HoltWintersParams =
        
        let hwOptim = (fun alpha beta gamma -> 
            let res = TripleHWT data seasonLength nbForecastSteps alpha beta gamma
            let rmse = RMSE res.[0..data.Length-1] data
            rmse
        )

        //initial parameters values, updated with the optimal values afterwards
        let mutable values = [|0.5; 0.4; 0.6|]        
        let mutable state : alglib.minbleicstate = null
        let mutable rep : alglib.minbleicreport = null

        let optimFunc : alglib.ndimensional_func = new alglib.ndimensional_func(fun x funcRes obj -> 
            funcRes <- hwOptim x.[0] x.[1] x.[2]
        )

        alglib.minbleiccreatef(3, values, 1.0e-6, &state)
        alglib.minbleicsetbc(state, [|0.001; 0.001; 0.001|], [|0.999; 0.999; 0.999|])
        alglib.minbleicsetcond(state, 0.0000000001, 0.0, 0.0, 0);
        alglib.minbleicoptimize(state, optimFunc, null, null);
        alglib.minbleicresults(state, &values, &rep);

        {
            alpha = values.[0];
            beta = values.[1];
            gamma = values.[2];
        }
