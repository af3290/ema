namespace MarketModels.Tests

module StochasticProcessesTests =
    open System
    open TestData
    open Microsoft.FSharp.Core    
    open NUnit.Framework
    open NUnit.Framework.Interfaces
    open MarketModels.MathFunctions
    open MarketModels.Simulations
    open MarketModels.Preprocess
    open MarketModels.Estimation

    let areWithinPrc (v1 : float) (v2 : float) (prc : float) : bool =
        (1.0 - prc) < (v1 / v2) && (v1 / v2) < (1.0 + prc)

    [<TestFixture>]
    type StochasticProcessesUnitTests() = 
        member this.S = [|3.0000;1.7600;1.2693;1.1960;0.9468;0.9532;0.6252;0.8604;1.0984;1.4310;1.3019;1.4005;1.2686;0.7147;0.9237;0.7297;0.7105;0.8683;0.7406;0.7314;0.6232|];
        member this.delta = 0.25

        [<Test>]
        member this.OUEst() = 
            let res = OU_MLE this.S this.delta

            Assert.IsTrue(areWithinPrc res.mu 0.9074 0.1)
            Assert.IsTrue(areWithinPrc res.sigma 0.5830 0.1)
            Assert.IsTrue(areWithinPrc res.lambda 3.1287 0.1)
