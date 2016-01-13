namespace MarketModels

module Types =
    open Microsoft.FSharp.Reflection

    type EquilibriumAlgorithm = CurveIntersection | WelfareMaximization

    type EquilibriumFill = Demand | Supply | Middle //also used for how the curves are drawn

    type SettlementMethod = PayAsBid | UniformPricing

    type ForecastMethod = Naive | AR | ARX | ARwGARCH  

    type Resolution = Yearly | Quarterly | Monthly | Weekly | Daily | Hourly

    type TimeHorizon = DayAhead | WeekAhead | MonthAhead | QuarterAhead | YearAhead

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
