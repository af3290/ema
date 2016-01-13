namespace MarketModels

module GasValuation =
    //TODO: port from matlab... yes, awesome...
    open System
    open System.Collections.Generic    
    open Microsoft.SolverFoundation.Services
    open Microsoft.SolverFoundation.Common
    open MathNet.Numerics
    open SolverExtensions

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