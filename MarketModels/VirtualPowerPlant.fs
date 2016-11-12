namespace MarketModels

module VirtualPowerPlant =
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

    ///Using finite difference methods... really basic... see hpcquantlib.wordpress.com...
    type VirtualPowerPlant() =
         // model definition
        let mutable beta         = 200;
        let mutable eta          = 1.0/0.4;
        let mutable lambda       = 4.0;
        let mutable alpha        = 7.0;
        let mutable volatility_x = 1.4;
        let mutable kappa        = 4.45;
        let mutable volatility_u = sqrt(1.3);
        let mutable rho          = 0.7;
        let mutable irRate       = 0.00;

        // vpp definition
        let mutable maturityInWeeks = 52;
        let mutable efficiencies = [| 0.3; 0.35; 0.4; 0.45; 0.5; 0.55; 0.6|];

        let mutable pMin             = 25;   // MWh
        let mutable pMax             = 40;   // MWh
        let mutable startUpFuel      = 20;   // MWh
        let mutable startUpFixedCost = 300;  // EUR
        let mutable carbonPrice      = 3.0;  // EUR per MWh heat (from gas)
        let mutable minOn            = 2.0;  // hour
        let mutable minOff           = 2.0;  // hour
        let mutable nStarts          = maturityInWeeks/2;

        member this.Solve() =
            //etc... todo solve this shit... yeah..
            ()
