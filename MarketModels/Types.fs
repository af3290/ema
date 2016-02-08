namespace MarketModels

///Contains all domain specific types and basic methods
module Types =
    open System
    open System.Collections
    open Microsoft.FSharp.Reflection

    type EquilibriumAlgorithm = CurveIntersection | WelfareMaximization

    type EquilibriumFill = Demand | Supply | Middle //also used for how the curves are drawn

    type SettlementMethod = PayAsBid | UniformPricing

    type ForecastMethod = Naive | HoltWinters | ARMA

    type Resolution = Yearly | Quarterly | Monthly | Weekly | Daily | Hourly

    type TimeHorizon = DayAhead | WeekAhead | MonthAhead

    //CONSTANTS...
    let DAY_PEAK_HOURS = [seq { 6..10 }; seq { 16..20 }] |> Seq.concat |> Seq.toArray

    let DAY_BASE_HOURS = [seq { 0..5 }; seq { 11..15 }; seq { 21..23 }] |> Seq.concat |> Seq.toArray

    let PI = Math.PI

    let GetTimeHorizonValue (ithTimeHorizon:int) = 
        match ithTimeHorizon with
        | 0 -> 24
        | 1-> 24*7
        | 2 -> 24*7*4//depending on current month...
        | _ -> 0

    ///Returns the case name of the object with union type 'ty.
    let GetUnionCaseName (x:'a) = 
        match FSharpValue.GetUnionFields(x, typeof<'a>) with
        | case, _ -> case.Name  

    ///Returns the case names of union type 'ty.
    let GetUnionCaseNames<'ty> () = 
        FSharpType.GetUnionCases(typeof<'ty>) |> Array.map (fun info -> info.Name)

    ///Returns the equivalent union from its name, throws exception if not found...
    let GetUnionCaseFromName<'ty> (name : string) = 
        let unionCase = FSharpType.GetUnionCases typeof<'ty> |> Array.find (fun case -> case.Name = name)
        FSharpValue.MakeUnion(unionCase,[||]) :?> 'ty
    
    ///May leave aside extra data that doesn't fit in a day
    ///sub periods must be defined in increasing order and well bounded
    let GetSubPeriodsFrom (series : float[]) (period : int) (subPeriodIndices : int[]): float[] =
        if subPeriodIndices.Length = 0 || subPeriodIndices.[0] < 0 || subPeriodIndices |> Seq.last > period then
            failwith "Can't be done"

        let periods = series.Length / period

        Array.init (periods * subPeriodIndices.Length) (fun i -> 
            series.[ (i / subPeriodIndices.Length) * period + subPeriodIndices.[ i % subPeriodIndices.Length ]]
        )