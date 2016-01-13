namespace MarketModels

module MarketClearing =
    open System
    open System.Collections.Generic    
    open Microsoft.SolverFoundation.Services
    open Microsoft.SolverFoundation.Common
    open MathNet.Numerics
    open SolverExtensions

    type OptimResult = {
        demandQuantities : float[]
        supplyQuantities : float[]
        cstrs : float[] 
        opVal : float
        eqQuantity : float
        eqPrice : float
        totalTradedQuantity : float
    }
    
    //always used Floor, but since LP can maximize to a middle value
    //between 2 offers, we can theoretically use it too...
    type EquilibriumMatching = Floor | Middle | Ceil

    //should typically be Demand, since we don't want to run out of electricity...
    type FirstFill = Demand | Supply

    //don't use any complex types? why?...
    //[0] - quantities, [1] - prices...
    //uses supply floor first matching
    //should be: welfare maximize => then drop to provided FirstFill thingy...
    let FindEquilibrium (supplyCurve : float[,]) (demandCurve : float[,]) : OptimResult =
        let context = SolverContext.GetContext()

        let model =  context.CreateModel()

        let sLength = supplyCurve.GetLength(1)
        let dLength = demandCurve.GetLength(1)

        for i in 0 .. sLength - 1 do
            let d = new Decision(Domain.RealNonnegative, "SupplyVar"+i.ToString())
            model.AddDecision(d)            
            model.AddConstraint("UpperConstraintSupply"+i.ToString(),  d <<== supplyCurve.[0, i]) |> ignore

        for i in 0 .. dLength - 1 do
            let d = new Decision(Domain.RealNonnegative, "DemandVar"+i.ToString())
            model.AddDecision(d)            
            model.AddConstraint("UpperConstraintDemand"+i.ToString(),  d <<== demandCurve.[0, i]) |> ignore
         
        let supplyVars = model.Decisions |> Seq.take sLength |> Seq.toArray
        let demandVars = model.Decisions |> Seq.skip sLength |> Seq.take dLength |> Seq.toArray

        let s1 = (DecisionsSum supplyVars)
        let s2 = (DecisionsSum demandVars)

        model.AddConstraint("Balance", s1 - s2 === 0.0) |> ignore

        let smax1 = DecisionsSumConstProduct supplyVars supplyCurve.[1, *]
        let smax2 = DecisionsSumConstProduct demandVars demandCurve.[1, *]

        let welfare = model.AddGoal("goal", GoalKind.Maximize, smax2 - smax1) 

        let dir = new SimplexDirective()
        dir.GetSensitivity <- true
        let solution = context.Solve(dir);
        let report = solution.GetReport(ReportVerbosity.All);

        let lrep = report :?> LinearReport;

        let sp = lrep.GetAllShadowPrices();
        let gs = lrep.GetAllConstraintBoundsSensitivity();
        let gsx = lrep.GetAllConstraintBoundsSensitivity();

        let sval = supplyVars |> Seq.map (fun x -> x.GetDouble()) |> Seq.sum;

        //should do the dual problem, but it takes more time
        //so we'll do intersection ourselves, since it's only 1 point
        let supplyCumQuantity = supplyCurve.[0, *] |> CumSum

        //matchingMethod = floor => get the ceiling, the first match 
        //could also do middle or ceiling...
        //if 0 => set to 0...
        let i = IndexOfLastLessOrEqualThan supplyCumQuantity sval

        let x = {
            demandQuantities = demandVars |> Seq.map (fun x -> x.GetDouble()) |> Seq.toArray;
            supplyQuantities = supplyVars |> Seq.map (fun x -> x.GetDouble()) |> Seq.toArray;
            cstrs = sp |> Seq.map (fun x -> x.Value.ToDouble()) |> Seq.toArray;
            opVal = welfare.ToDouble();
            eqQuantity = supplyCurve.[0, i];
            eqPrice = supplyCurve.[1, i];
            totalTradedQuantity = sval
        } 

        //clear or cache? use the F# ideas... YES...
        context.ClearModel()

        //equilibrium solved in like 1s, too slow, yes!

        x

    type SettlementType = PayAsBid | UniformPricing
  
    type SettlementResult = {
        //settlement values (price*quantity) for each side
        demand : float[]
        supply : float[]
    }

    let Settle (supplyCurve : float[,]) (demandCurve : float[,]) (supplyQs : float[]) (demandQs : float[,]) : SettlementResult =
        
        let x = {
            demand = [|1.0|];
            supply = [|1.0|];
        }

        x

    let IntersectByFittingPolynoms (demandCurve : float[,]) (supplyCurve : float[,]) : int =
        let minLen = min(demandCurve.GetLength(1), supplyCurve.GetLength(1))
        // different lenghts, find minimum in between, we assume the  intersection to be somewhere in the middle

        let dPolynom = Fit.Polynomial(demandCurve.[0, *], demandCurve.[1, *], demandCurve.GetLength(1))
        let sPolynom = Fit.Polynomial(supplyCurve.[0, *], supplyCurve.[1, *], supplyCurve.GetLength(1))

        //TODO: figure this out later...
        1
