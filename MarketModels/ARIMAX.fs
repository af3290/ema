namespace MarketModels

///Holds ARIMAX objects and functionality
module ARIMAX = 
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

    //TODO: put all data here...
