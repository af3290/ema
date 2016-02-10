namespace MarketModels

module GARCH = 
    open System
    open System.Collections.Generic        

    open MathNet.Numerics
    open MathNet.Numerics.Statistics
    open MathNet.Numerics.LinearAlgebra
    open MathNet.Numerics.LinearAlgebra.Double
    open MathNet.Numerics.LinearAlgebra.Matrix

    open Operations
    open MathFunctions    
    open Optimization
    open Forecast
    open TimeSeries

    ///A GARCH model, etc...
    type GARCH (GARCH : LagOp, ARCH : LagOp) = 
        let mutable x = 0.1

        member this.Evaluate (series : float[]) : float[] = 
            series