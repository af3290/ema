﻿namespace MarketModels.Tests

module TimeSeriesTests =
    open Microsoft.FSharp.Core
    open NUnit.Framework
    open NUnit.Framework.Interfaces
    open MarketModels.MathFunctions
    open MarketModels.TimeSeries

    [<TestFixture>]
    type TimeSeriesUnitTests() = 

        [<Test>]
        member this.FilterTest1() = 
            //artificial data... etc...
            let Y = [|31.9701258398659; 31.0566057478547; 30.0367977830021; 29.4098534693792; 29.2309678510293; 31.4362523770192; 33.6074513348075; 34.6330454500176; 35.7398136451162; 35.5879742939911; 36.2526361638673; 37.7327296685769; 38.8246049520236; 40.0316277476803; 39.0854608193705; 40.1613426766006; 38.7270195691525; 36.5136465205741; 37.4329976892467; 35.7559158058523; 33.9969915079280; 33.7739829827230; 33.3572337240452; 36.6331473600124; 36.3233883115212; 35.6772867067768; 35.6475318772809; 35.0404464399008; 32.0402427943400; 30.5886972684373; 31.9561430865081; 43.7072318925434; 50.3878139394647; 46.2861926242720; 39.9426607038115; 34.3388303357668; 34.4900109240278; 33.1642396110154; 34.5408618964680; 32.3645230577274; 31.0893785217079; 31.7492563016660; 39.4463363862639; 37.6563949943225; 36.5676593410869; 34.5402560618265; 34.7670407731469; 37.1002522025905; 39.6363224428661; 39.4347003645476; 38.6990748412613; 38.3230865606927; 37.0858721550586; 36.9941358041080; 37.8794528392554; 37.8305459078931; 36.2741688038464; 36.1309635666233; 36.3998035936597; 37.6676136189519; 39.0305092342493; 39.6313010971483; 40.1266041561924; 40.6332952537853; 40.3896025209229; 40.0567147222137; 40.2485037288420; 40.1801777651232; 39.2652783232952; 36.9575056148528; 36.2093349799322; 34.7617644671117; 33.4726597335516; 33.7700219457441; 33.8688239473140; 35.5029992700072; 37.2445322370402; 38.2371204761820; 37.0997253405636; 36.6371814499304; 35.2433904264698; 34.4816521022939; 35.8599732583205; 36.0474310683675; 41.4054513239057; 38.9580506392728; 43.2950873456220; 42.5980940611288; 37.1611165733388; 33.7139860352261; 31.6206828671640; 30.7446328606229; 32.7252901626463; 35.9269209515003|];
            
            let resf = Filter1D [|1.0; -1.2106; 0.5433; 0.1233|] [| 1.0; 0.5 |] Y

            Assert.AreEqual(resf.[0], Y.[0])

        [<Test>]
        member this.FilterTest2() = 
            //data taken from matlab...
            let Y = [|31.9701258398659; 31.0566057478547; 30.0367977830021; 29.4098534693792; 29.2309678510293; 31.4362523770192; 33.6074513348075; 34.6330454500176; 35.7398136451162; 35.5879742939911; 36.2526361638673; 37.7327296685769; 38.8246049520236; 40.0316277476803; 39.0854608193705; 40.1613426766006; 38.7270195691525; 36.5136465205741; 37.4329976892467; 35.7559158058523; 33.9969915079280; 33.7739829827230; 33.3572337240452; 36.6331473600124; 36.3233883115212; 35.6772867067768; 35.6475318772809; 35.0404464399008; 32.0402427943400; 30.5886972684373; 31.9561430865081; 43.7072318925434; 50.3878139394647; 46.2861926242720; 39.9426607038115; 34.3388303357668; 34.4900109240278; 33.1642396110154; 34.5408618964680; 32.3645230577274; 31.0893785217079; 31.7492563016660; 39.4463363862639; 37.6563949943225; 36.5676593410869; 34.5402560618265; 34.7670407731469; 37.1002522025905; 39.6363224428661; 39.4347003645476; 38.6990748412613; 38.3230865606927; 37.0858721550586; 36.9941358041080; 37.8794528392554; 37.8305459078931; 36.2741688038464; 36.1309635666233; 36.3998035936597; 37.6676136189519; 39.0305092342493; 39.6313010971483; 40.1266041561924; 40.6332952537853; 40.3896025209229; 40.0567147222137; 40.2485037288420; 40.1801777651232; 39.2652783232952; 36.9575056148528; 36.2093349799322; 34.7617644671117; 33.4726597335516; 33.7700219457441; 33.8688239473140; 35.5029992700072; 37.2445322370402; 38.2371204761820; 37.0997253405636; 36.6371814499304; 35.2433904264698; 34.4816521022939; 35.8599732583205; 36.0474310683675; 41.4054513239057; 38.9580506392728; 43.2950873456220; 42.5980940611288; 37.1611165733388; 33.7139860352261; 31.6206828671640; 30.7446328606229; 32.7252901626463; 35.9269209515003|];
            let result = [|31.9701258398659; -7.64557897847645; 9.81076487092602; 9.92203181513606; 9.94801796778196; 12.0292201882761; 11.4335667165928; 11.0289581899588; 12.0738428728465; 11.1394134895206; 12.5892263357701; 13.1821992537807; 12.8434421647579; 13.5328461943673; 11.7187361018659; 14.5958319355031; 11.3449957363468; 11.4525330009900; 14.2720266703725; 10.2794148561517; 11.0502306680233; 12.0453233914741; 10.9428692784185; 14.6021216868775; 10.1001970539149; 11.6089775491069; 12.1930753476076; 11.2709651397907; 8.98951635540043; 10.8400910364750; 12.3346442585213; 21.6416739822037; 14.8396719874796; 9.03541268360689; 11.2869426721165; 11.1338972292516; 14.6223066057728; 10.0688014191681; 13.1325078896097; 8.56933784878235; 10.6767688852915; 11.6978366431142; 17.9032643283219; 7.15397249790237; 12.4141324494238; 10.7321981103584; 12.8217626468801; 13.7788893004932; 13.6136543030269; 11.6096323899938; 12.4960034961250; 12.9009969779339; 11.7192579532827; 12.9209754733172; 13.2451327083767; 12.0746415618670; 11.0584875378073; 12.7728185500383; 12.3693955710181; 13.2339475704272; 13.2081344105132; 12.8478779103052; 13.3563778315130; 13.5898953276529; 13.0019279020442; 13.2393475757505; 13.7017165882215; 13.2203486449683; 12.4923671612387; 11.2550239853002; 12.8034910592081; 11.0077582055074; 11.0645417136472; 12.1359540791084; 11.1743704256531; 12.8505039254302; 12.6674295821861; 12.4396577143065; 11.0468854838989; 12.5005430290343; 11.0487165759585; 11.7229515175376; 13.2661269759000; 11.3711519650126; 17.2511214328127; 8.41929491904165; 18.6302529763273; 11.3532166773698; 9.11643477760740; 11.8724686395130; 10.9981072845866; 10.7832314762326; 12.6870611404071; 13.0149782246981 |];
            
            let resf = Filter1D [|1.0; -1.2106; 0.5433|] [| 1.0 |] Y

            Assert.AreEqual(resf.[0], Y.[0], 0.001)

            for i in 1..10 do
                Assert.AreEqual(resf.[i], result.[i], 0.01)

            Assert.AreEqual(resf.Length, Y.Length)
            Assert.AreEqual(resf.Length, result.Length)

        [<Test>]
        member this.ARMATest1() = 
            let Y = [|31.9701258398659; 31.0566057478547; 30.0367977830021; 29.4098534693792; 29.2309678510293; 31.4362523770192; 33.6074513348075; 34.6330454500176; 35.7398136451162; 35.5879742939911; 36.2526361638673; 37.7327296685769; 38.8246049520236; 40.0316277476803; 39.0854608193705; 40.1613426766006; 38.7270195691525; 36.5136465205741; 37.4329976892467; 35.7559158058523; 33.9969915079280; 33.7739829827230; 33.3572337240452; 36.6331473600124; 36.3233883115212; 35.6772867067768; 35.6475318772809; 35.0404464399008; 32.0402427943400; 30.5886972684373; 31.9561430865081; 43.7072318925434; 50.3878139394647; 46.2861926242720; 39.9426607038115; 34.3388303357668; 34.4900109240278; 33.1642396110154; 34.5408618964680; 32.3645230577274; 31.0893785217079; 31.7492563016660; 39.4463363862639; 37.6563949943225; 36.5676593410869; 34.5402560618265; 34.7670407731469; 37.1002522025905; 39.6363224428661; 39.4347003645476; 38.6990748412613; 38.3230865606927; 37.0858721550586; 36.9941358041080; 37.8794528392554; 37.8305459078931; 36.2741688038464; 36.1309635666233; 36.3998035936597; 37.6676136189519; 39.0305092342493; 39.6313010971483; 40.1266041561924; 40.6332952537853; 40.3896025209229; 40.0567147222137; 40.2485037288420; 40.1801777651232; 39.2652783232952; 36.9575056148528; 36.2093349799322; 34.7617644671117; 33.4726597335516; 33.7700219457441; 33.8688239473140; 35.5029992700072; 37.2445322370402; 38.2371204761820; 37.0997253405636; 36.6371814499304; 35.2433904264698; 34.4816521022939; 35.8599732583205; 36.0474310683675; 41.4054513239057; 38.9580506392728; 43.2950873456220; 42.5980940611288; 37.1611165733388; 33.7139860352261; 31.6206828671640; 30.7446328606229; 32.7252901626463; 35.9269209515003|];
            
            let res = ARMA Y 2 1

            Assert.AreEqual(4, 2+2)

        [<Test>]
        member this.ARMATest2() = 
            let Y = [||];
            
            let res = ARMA Y 2 1

            Assert.AreEqual(4, 2+2)

        [<Test>]
        member this.ARMATest3() = 
            //simple trend... etc...
            let Y = [|1.0; 2.0; 3.0; 4.0;|];
            
            let res = ARMA Y 2 1

            Assert.AreEqual(4, 2+2)