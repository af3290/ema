namespace MarketModels.Tests
open Microsoft.FSharp.Core
open NUnit.Framework
open NUnit.Framework.Interfaces

module module1 =
    let inline public Square x = x * x;

    [<TestFixture>]
    type TestClass() = 

        [<Test>]
        member this.When2IsAddedTo2Expect4() = 
            Assert.AreEqual(4, 2+2)