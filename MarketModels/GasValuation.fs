namespace MarketModels

module GasValuation =
    //TODO: for future development and inclusion into our problem... yeah...
    open System
    open System.Collections.Generic    

    open Microsoft.SolverFoundation.Services
    open Microsoft.SolverFoundation.Common
    open MathNet.Numerics
    
    open MathFunctions
    open Operations
    open SolverExtensions

    type Polynomial = {
        Coefficients : float[];
    } with
        member this.ValueAt(x : float) = 
            let Xs =  Array.init (this.Coefficients.Length) (fun i -> x ** ((float)i))
            this.Coefficients .* Xs |> sum

    (* American Option/Least Squares Monte Carlo Valuation follows... *)
    type StorageSpecifications = {
        //Volume : float
        InitialFill : float
        MaxFills : float[]
        MinFills : float[]
        InjectionSpeed : float
        WithdrawalSpeed : float
        //ÏnjectionCost : float
        //WithDrawalCost : float
        //StartDate : DateTime
        //EndDate : DateTime

    }

    type StorageValuationResult = {

        Specifications : StorageSpecifications
        StorageDiscretization : float[]
        Simulations: float[][]
        Values: float[][][]
        Polynomials: Polynomial[][]
        OptimalDeltaFills : float[][][]

    }

    let estimatePolynomials (storageValsNextPeriod : float[][]) (currentPrices : float[]) (discountFactor : float) (polynomialLength : int) =        
        let polynomials = storageValsNextPeriod |> Array.map(fun svsSims -> 
            let coeffs = Fit.Polynomial(currentPrices, svsSims |> Array.map(fun yValue -> exp(-discountFactor)*yValue), polynomialLength)
            {Coefficients = coeffs}
        )   
        polynomials 

    let storageDescretization maxFill minFill storageVolGranularity = 
        (maxFill - minFill)/((float) (storageVolGranularity-1))

    let storageFromDescretization storageVolGranularity deltaFill = 
        Array.init storageVolGranularity (fun i -> (float) i*deltaFill)

    let calculateMaxVolume (storageVol : float) (maxFill : float) (maxInjectionSpeed : float) =
        storageVol + min (maxFill-storageVol) maxInjectionSpeed    

    let calculateMinVolume (storageVol : float) (minFill : float) (maxWithdrawalSpeed : float) =
        storageVol + max (minFill-storageVol) (-maxWithdrawalSpeed)
    
    /// This function calculates how much can be injected and withdrawn (negativ injection) for all storagelevels 
    /// at a given timepoint (Returns the possible min and max volumes after an action)
    let calculateMaxMinInjections (storageVols : float[]) (minFill : float) (maxFill : float) (maxInjectionSpeed : float) (maxWithdrawalSpeed : float) = 
        // Have length according to storage granularity
        let maxVolumeAfterAction = storageVols |> Array.map(fun vol -> calculateMaxVolume vol maxFill maxInjectionSpeed) 
        let minVolumeAfterAction = storageVols |> Array.map(fun vol -> calculateMinVolume vol minFill maxWithdrawalSpeed)

        (minVolumeAfterAction, maxVolumeAfterAction) //Maximum Injections and withdrawals for each volume at a given time

    let calculateProfit (price : float) (deltaVol : float) (polynomial : Polynomial) (injectionCost : float) (withdrawalCost : float) (bidAskSpread : float) (exceedingStorageLimitsPenalty : float) = 
        
        let tradeCost = bidAskSpread/2.*abs(deltaVol)
        let storageChangeCost = if deltaVol > 0. then injectionCost*deltaVol else -withdrawalCost*deltaVol
         
        let polVal = polynomial.ValueAt(price)
        let profit = -price*deltaVol + polVal - tradeCost - storageChangeCost
        let res =
            if Double.IsNaN(profit) then
                exceedingStorageLimitsPenalty
            else profit
        res

    /// Function that calculates the feasible next volume-granularities (And corresponding profits) given a current volume 
    /// (Including upper and lower bounds which are, probably, unfeasible)
    let calculateVolsAndProfits (price : float) (injectionCost : float) (withdrawalCost : float) (bidAskSpread : float) (storageVols : float[]) (polynomials : Polynomial[]) (currentVol : float) (boundIndexes : (int*int)) (exceedingStorageLimitsPenalty : float) = 
        let possibleVolsAndPols = 
            if (fst(boundIndexes) = snd(boundIndexes)) then 
                [|(currentVol, exceedingStorageLimitsPenalty)|]
            else
                let maxVol = storageVols.[snd boundIndexes]
                let minVol = storageVols.[fst boundIndexes]

                (storageVols, polynomials) ||> Array.zip |> Array.filter(fun (storageVol, polynomial) ->  minVol <= storageVol && storageVol <= maxVol) |> Array.map(fun (storageVol, polynomial) -> (storageVol, calculateProfit price (storageVol - currentVol) polynomial injectionCost withdrawalCost bidAskSpread exceedingStorageLimitsPenalty))
        possibleVolsAndPols

    /// Function that calculates the upper and lower bounds (returns the indexes) of feasible values in an array 
    ///(The calculated indexbounds are for values that are unfeasible, thus not being in the wanted interval)
    let getBoundsIndexes (storageVols : float[]) (minPossibleVol : float) (maxPossibleVol : float) = 
        let lastVolIndex = (storageVols |> Array.length)-1
        //FindLastIndex returns -1 if no value is found
        let upperVolBoundIndex = Array.FindLastIndex(storageVols, (fun vol -> vol < maxPossibleVol))+1 |> min lastVolIndex
        let lowerVolBoundIndex = Array.FindLastIndex(storageVols, (fun vol -> vol < minPossibleVol)) |> max 0
        
        (lowerVolBoundIndex, upperVolBoundIndex)

    // Function that calculates the optimal strategy given a price and a storage volume
    let calculateOptimalFeasibleChoice (currentPrice : float) (injectionCost : float) (withdrawalCost : float) (bidAskSpread : float) (storageVols : float[]) (polynomials : Polynomial[]) (currentVol : float) (exceedingStorageLimitsPenalty : float) (minPossibleVol : float) (maxPossibleVol : float)  = 
        let boundIndexes = getBoundsIndexes storageVols minPossibleVol maxPossibleVol
        let possibleVolsAndProfits = calculateVolsAndProfits currentPrice injectionCost withdrawalCost bidAskSpread storageVols polynomials currentVol boundIndexes exceedingStorageLimitsPenalty
        let optimalChoice = possibleVolsAndProfits |> Array.maxBy(snd)

        let optimalVol = fst optimalChoice
//        let a = if optimalVol > 90.    then
//                                            3.
//                                        else
//                                            0.

        let optimalProfit = snd optimalChoice
        
        let optimalDeltaVolAndProfit = //let optimalVolAndProfit = 
            if possibleVolsAndProfits.Length > 1 then
                if optimalVol < minPossibleVol then
                    let possibleTuple = possibleVolsAndProfits.[1]
                    let possibleVol = fst possibleTuple
                    let possibleProfit = snd possibleTuple
                    let weight = (minPossibleVol-optimalVol)/(possibleVol-optimalVol)
                    
                    let vol = (1.-weight)*optimalVol+weight*possibleVol
                    let deltaVol = vol-currentVol
                    let profit = (1.-weight)*optimalProfit+weight*possibleProfit
                    (vol-currentVol, profit)//(vol, profit)
                elif optimalVol > maxPossibleVol then
                    let lastIndex = (possibleVolsAndProfits |> Array.length)-1
                    let possibleTuple = possibleVolsAndProfits.[lastIndex-1]
                    let possibleVol = fst possibleTuple
                    let possibleProfit = snd possibleTuple
                    let weight = (optimalVol-maxPossibleVol)/(optimalVol-possibleVol)

                    let vol = (1.-weight)*optimalVol+weight*possibleVol
                    let deltaVol = vol-currentVol
                    let profit = (1.-weight)*optimalProfit+weight*possibleProfit
                    (vol-currentVol, profit)//(vol, profit)
                else (optimalVol-currentVol, optimalProfit)//optimalChoice
            else (optimalVol-currentVol, optimalProfit)//optimalChoice
        optimalDeltaVolAndProfit//optimalVolAndProfit
    


    let calculateProfitsAndVolsForAllPrices (currentPrices : float[]) (injectionCost : float) (withdrawalCost : float) (bidAskSpread : float) (storageVols : float[]) (polynomials : Polynomial[]) (currentVol : float) (exceedingStorageLimitsPenalty : float) (minPossibleVol : float) (maxPossibleVol : float)  = 
        currentPrices |> Array.map(fun currentPrice -> calculateOptimalFeasibleChoice currentPrice injectionCost withdrawalCost bidAskSpread storageVols polynomials currentVol exceedingStorageLimitsPenalty minPossibleVol maxPossibleVol)
        
    let calculateProfitsAndVolsForAllPricesAndVolumes (currentPrices : float[]) (injectionCost : float) (withdrawalCost : float) (bidAskSpread : float) (storageVols : float[]) (polynomials : Polynomial[]) (exceedingStorageLimitsPenalty : float) (minPossibleVols : float[]) (maxPossibleVols : float[])  = 
        storageVols |> Array.mapi(fun i currentVol -> calculateProfitsAndVolsForAllPrices currentPrices injectionCost withdrawalCost bidAskSpread storageVols polynomials currentVol exceedingStorageLimitsPenalty minPossibleVols.[i] maxPossibleVols.[i])
    
    let storageValuation (simulationMatrix : float[,]) (storageSpecifications : StorageSpecifications) (storageVolGranularity : int) (discountFactor : float) = 
        // minFillVector and maxFillVector specifies the volumeband at each timepoint

        //*********************************** Things to be set as parameters *****************************************//
        let notEmptyPenalty = Double.NegativeInfinity //-100000.
        let notWithinBandsPenalty = Double.NegativeInfinity //-100000.
        let polynomialLength = 3

        let injectionCost = 0.//0.5
        let withdrawalCost = 0.//0.5
        let bidAskSpread = 0. //1.
        //************************************************************************************************************//
        let minFillVector = storageSpecifications.MinFills
        let maxFillVector = storageSpecifications.MaxFills
        let minFill = minFillVector |> Array.min
        let maxFill = maxFillVector |> Array.max
        
        let maxInjectionSpeed = storageSpecifications.InjectionSpeed
        let maxWithdrawalSpeed = storageSpecifications.WithdrawalSpeed

        let timepoints = (simulationMatrix |> Array2D.length2)
        let numOfSims = (simulationMatrix |> Array2D.length1)

        let deltaFill = storageDescretization maxFill minFill storageVolGranularity

        let storageVols = storageFromDescretization storageVolGranularity deltaFill 
//        let storageVols = makeGranularity maxFill maxInjectionSpeed maxWithdrawalSpeed
        
        // Manipulating the simulations array such that it is a jaggedArray with the first entry is an array of simulations for the last timepoint and the last entry is an array of simulations for the first timepoint. This is done because it is needed for the backward induction regression
        let sims = JaggedArray.fromArray2D simulationMatrix |> JaggedArray.transpose |> Array.rev 

        let polynomials : Polynomial[][] = Array.init 1 (fun i -> Array.zeroCreate storageVolGranularity)
        

        let endY = Array.init storageVolGranularity (fun i ->   if i = 0 then Array.init numOfSims (fun j -> (0., 0.))//Array.zeroCreate numOfSims
                                                                else Array.init numOfSims (fun j -> (0., notEmptyPenalty)))
    
        let scanFunc = fun (yVals, polynomialArr) (minFill, maxFill, currentPrices) -> 
                let polysForCurrentT = estimatePolynomials (yVals |> JaggedArray.map(snd)) currentPrices discountFactor polynomialLength
                let nextPolArr = Array.append polynomialArr (Array.init 1 (fun i -> polysForCurrentT))
                                                                                                                                                            
                //calculateProfitsFromOptimalDescisions polysForCurrentT currentPrices storageVols minFill maxFill maxInjectionSpeed maxWithdrawalSpeed notWithinBandsPenalty
                let minAndMaxVols = calculateMaxMinInjections storageVols minFill maxFill maxInjectionSpeed maxWithdrawalSpeed 
                let minVols = fst minAndMaxVols
                let maxVols = snd minAndMaxVols
                                                                                                                                            
                let res = calculateProfitsAndVolsForAllPricesAndVolumes currentPrices injectionCost withdrawalCost bidAskSpread storageVols polysForCurrentT notWithinBandsPenalty minVols maxVols 
                (res, nextPolArr)

        let resultingY = (minFillVector, maxFillVector, sims) |||> Array.zip3 |> Array.scan scanFunc (endY, polynomials) |> Array.rev
        
        let resultPols = resultingY.[(resultingY.Length)-1] |> snd |> Seq.skip 1 |> Seq.toArray //|> Array.rev
        let resultDeltaVolsAndProfits = resultingY |> Array.map(fst) |> Seq.skip 1 |> Seq.toArray //|> Array.rev
        let resultDeltaVols = resultDeltaVolsAndProfits |> Array.map(JaggedArray.map(fst))
        let resultVals = resultDeltaVolsAndProfits |> Array.map(JaggedArray.map(snd))

        {
            Specifications = storageSpecifications; 
            StorageDiscretization = storageVols; 
            Simulations = sims; 
            Values = resultVals; 
            Polynomials = resultPols; 
            OptimalDeltaFills = resultDeltaVols
        }
        
    let optimalDeltaFillGivenCurrentFill (currentFill : float) (storageGranularity : float[]) (deltaFillsForEachVol : float[])=    
//        let minVolAfterAction = calculateMinVolume currentFill minFill withdrawalSpeed
//        let maxVolAfterAction = calculateMaxVolume currentFill maxFill injectionSpeed
        let boundIndexes = getBoundsIndexes storageGranularity currentFill currentFill
        let lBoundIndex = fst boundIndexes
        let uBoundIndex = snd boundIndexes
        let lBound = storageGranularity.[lBoundIndex]
        let uBound = storageGranularity.[uBoundIndex]
        let weight = if uBound=lBound then 0. else (uBound-currentFill)/(uBound-lBound)
        let optimalDeltaFill = weight*deltaFillsForEachVol.[lBoundIndex]+(1.-weight)*deltaFillsForEachVol.[uBoundIndex]
        optimalDeltaFill

    ///Optimal Filling strategy for a given price path... in TODO...
    let FillStrategyForPricePath (valuationResult : StorageValuationResult) (prices : float[]) = 
        let storageGranularity = valuationResult.StorageDiscretization
        let optimalDeltaFills = valuationResult.OptimalDeltaFills |> Array.map(JaggedArray.transpose)
        let startValue = valuationResult.Specifications.InitialFill
        let simPrices = valuationResult.Simulations

        let scanFunc = fun storageLevel (price, simPricesAtT, deltaFillsAtT) ->   
            let (sortedPrices, correspondingDeltaFills) = (simPricesAtT, deltaFillsAtT) ||> Array.zip |> Array.sortBy(fst) |> Array.unzip                                                                                                                                        
            let (lowerPriceIndex, upperPriceIndex) = getBoundsIndexes sortedPrices price price
                                                                                                                                        
            let lowerPrice = sortedPrices.[lowerPriceIndex]
            let upperPrice = sortedPrices.[upperPriceIndex]

            let lowerDeltaFill = correspondingDeltaFills.[lowerPriceIndex] |> optimalDeltaFillGivenCurrentFill storageLevel storageGranularity 
            let upperDeltaFill = correspondingDeltaFills.[upperPriceIndex] |> optimalDeltaFillGivenCurrentFill storageLevel storageGranularity 

            let weight = if upperPrice > lowerPrice then (price - lowerPrice)/(upperPrice-lowerPrice) else 0.

            let resultingDeltaFill = (1.-weight)*lowerDeltaFill + weight*upperDeltaFill

            storageLevel + resultingDeltaFill
            (*
                THIS IS THE RIGHT ANSWER...
            let lowerLevelIndex = storageGranularity |> findLowerBoundOfValueInterval storageLevel
            let upperLevel = storageGranularity.[lowerLevelIndex+1]
            let lowerLevel = storageGranularity.[lowerLevelIndex]
            let weight = (upperLevel-storageLevel)/(upperLevel-lowerLevel)
            let optimalDelta = weight*deltaFills.[lowerLevelIndex]+(1.-weight)*deltaFills.[lowerLevelIndex+1]

            storageLevel+optimalDelta
            *)

        (prices, simPrices, optimalDeltaFills) |||> Array.zip3 |> Array.scan scanFunc 0.
                
    /// Calculates the optimal fills for a simulated price path
    let FillStrategyForSimulatedPricePath (valuationResult : StorageValuationResult) (pathIndex : int) = 
        let storageGranularity = valuationResult.StorageDiscretization
        
        let initialFill = valuationResult.Specifications.InitialFill

        let prices = (valuationResult.Simulations |> JaggedArray.transpose).[pathIndex] //prices over time
        let optimalDeltaFills = valuationResult.OptimalDeltaFills |> JaggedArray.map(fun simArr -> simArr.[pathIndex]) // time*volume 
        
        let diffCheck = optimalDeltaFills |> Array.map(fun arr -> if (Array.max(arr) - Array.min(arr))>5. then 0 else 1) |> Array.sum

        let storageSpecs = valuationResult.Specifications
        
        let minFill = storageSpecs.MinFills.[0] // FEJL!!
        let maxFill = storageSpecs.MaxFills.[0] // FEJL!!  Disse skal ind under res nedenUnder da min og max Fill kan være afhængig af tid.

        let injectionSpeed = storageSpecs.InjectionSpeed
        let withdrawalSpeed = storageSpecs.WithdrawalSpeed
        
        let scanFunc = fun currentFill (price : float, deltaFillsForEachVol : float[]) ->  
            let minVolAfterAction = calculateMinVolume currentFill minFill withdrawalSpeed
            let maxVolAfterAction = calculateMaxVolume currentFill maxFill injectionSpeed
            let boundIndexes = getBoundsIndexes storageGranularity currentFill currentFill
            let lBoundIndex = fst boundIndexes
            let uBoundIndex = snd boundIndexes
            let lBound = storageGranularity.[lBoundIndex]
            let uBound = storageGranularity.[uBoundIndex]
            let weight = if uBound=lBound then 0. else (uBound-currentFill)/(uBound-lBound)
            let optimalDeltaFill = weight*deltaFillsForEachVol.[lBoundIndex]+(1.-weight)*deltaFillsForEachVol.[uBoundIndex]
            let optimalDeltaFill = optimalDeltaFillGivenCurrentFill currentFill storageGranularity deltaFillsForEachVol
            optimalDeltaFill + currentFill
          
        //below is the correct one...                                                                                                                  
        //let res = (prices, optimalDeltaFills) ||> Array.zip |> Array.scan scanFunc initialFill


        let res = optimalDeltaFills |> Array.scan(fun currentFill deltaFillsForEachVol ->   let optimalDeltaFill = optimalDeltaFillGivenCurrentFill currentFill storageGranularity deltaFillsForEachVol
                                                                                            optimalDeltaFill + currentFill
                                                                                            ) initialFill

        res 

    // Function that calculates the optimal fills for all simulated price paths
    let FillStrategyForAllSimulatedPricePaths (valuationResult : StorageValuationResult) =
        let numOfSims = valuationResult.Simulations |> JaggedArray.transpose |> Array.length
        
        [|0..(numOfSims-1)|] |> Array.map(fun simIndex -> FillStrategyForSimulatedPricePath valuationResult simIndex)

    (* 
        Simple own approach based on LP programming follows... 
        TODO: to be finished later...
    *)

    type Storage = {       
        InjectionSpeed : float
        WithdrawalSpeed : float
        ÏnjectionCost : float
        WithDrawalCost : float
        StartDate : DateTime
        EndDate : DateTime
    }

    type Valuation = {       
        DiscountRate : float
        //etc...
    }

    type OptimResult = {
       
        totalTradedQuantity : float
    }

    let IntrinsicValuation (supplyCurve : float[,]) (demandCurve : float[,]) : OptimResult =
        let x = {
            totalTradedQuantity = 0.0
        } 
        
        //equilibrium solved in like 1s, too slow, yes!

        x