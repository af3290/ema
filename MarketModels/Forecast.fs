namespace MarketModels

module Forecast =
    open Operations
    open MathFunctions
    open MathNet.Numerics.Distributions
    open MathNet.Numerics.Statistics

    type SpikePreprocess = None | SimilarDay | Limited

    type ForecastResult = {
        Forecast : float[]
        Confidence : float[,] //because the inners have the same lengths! not array of arrays!
    }

    let Naive (data : float[]) (seasonalities : int[]) (forecastSteps : int) (alpha : float) : ForecastResult =
        //data provided at starting of first largest season...???
        //average from all seasonalities.. make some histogram... return 95%..
        
        let seasonalPeriods = 
            Array2D.init seasonalities.Length forecastSteps (fun i j -> data.[data.Length - seasonalities.[i] + j])

        let avg = Array.init forecastSteps (fun i -> seasonalPeriods.[*,i] |> Array.average)

        let res = {
                Forecast = avg;
                Confidence = Array2D.init 2 forecastSteps (fun i j -> 
                    if i = 0 
                    then seasonalPeriods.[*,j] |> Array.max 
                    else seasonalPeriods.[*,j] |> Array.min)
            }

        res

    let NaiveMultivariate (data : float[]) (predictors : float[,])  (forecastSteps : int) (alpha : float) : ForecastResult =
        
        let res = {
                Forecast = [| 0.0 |];
                Confidence = Array2D.init 2 forecastSteps (fun i j -> 0.0)
            }

        res

    type FitStatistics = {
        Bias : float  //mean error
        R : float
        IndependencePValue: float
        NormalityPValue: float
        //min, avg, max
        RSE : float[]
        APE : float[]
        MAPE : float
        RMSE : float
    }

    let ForecastFit (forecasted : float[]) (realized : float[]) : FitStatistics =
        let residuals = realized -- forecasted
        let ressd = Statistics.StandardDeviation residuals
        let resm = Statistics.Mean residuals

        let prcErrs = residuals./realized
        let sqErrs = residuals ^^ 2.0 |> Array.map sqrt
        
        let mutable normalityPValue = 0.0
        
        //determine the pValue for residuals' normality
        if forecasted.Length < 30 then
            //use T-Test
            let tval = resm / (ressd * sqrt((float)residuals.Length))
            let studt = new StudentT(0.0, ressd, (float)residuals.Length)
            let pVal = studt.Density(tval)
            normalityPValue <- pVal
        else
            //use Z-Test
            let zval = resm / ressd
            let norm = new Normal(resm, ressd)    
            let pVal = norm.Density(zval)
            normalityPValue <- pVal

        let autocorr = SeriesAutocorrelation residuals 23
        
        let res = {
            Bias = forecasted -- realized |> Array.average;
            R = MathNet.Numerics.GoodnessOfFit.RSquared(realized, forecasted)
            IndependencePValue = autocorr |> Array.max;
            NormalityPValue = normalityPValue;
            APE = [| prcErrs|>Array.min; prcErrs|>Array.average; prcErrs|>Array.max |];
            RSE = [| sqErrs|>Array.min; sqErrs|>Array.average; sqErrs|>Array.max |];
            MAPE = Array.sum prcErrs / (float)prcErrs.Length
            RMSE = sqrt(Array.sum sqErrs / (float)sqErrs.Length) 
        }

        res