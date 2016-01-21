emaDemos.controller('SensitivityAnalysisController', function ($scope, $http, blockUI) { // $blockUI
    /* UI parameters */
    $scope.date = new Date();
    $scope.sensitivityChangePercentage = 1; //0 to 100, since it's %, left undefined... first set in .on load

    /* Page parameters */
    $scope.MarketCurves = [];
    angular.extend($scope, SERVER_CONSTANTS);
    $scope.EquilibriumAlgorithm = $scope.EAs[0];
    $scope.EquilibriumFill = $scope.EFs[0];
    $scope.selectedHour = "Hours";
    
    var productionStackCum;
    var fuels;

    $('#hour-chart').highcharts({
        chart: {
            width: 750,
            height: 480,
            zoomType: 'xy'
        },
        title: {
            text: 'Demand and Supply Curves',
            x: -20 //center
        },
        subtitle: {
            text: 'Source: nordpoolspot',
            x: -20
        },
        xAxis: {
            title: {
                text: 'Volume (MWh)'
            }
        },
        yAxis: {
            title: {
                text: 'Price (EUR)'
            },
            plotLines: [{
                value: 0,
                width: 1,
                color: '#808080'
            }]
        },
        tooltip: {
            valueSuffix: ' EUR'
        },
        legend: {
            layout: 'vertical',
            align: 'right',
            verticalAlign: 'middle',
            borderWidth: 0
        },
        series: []
    });

    $('#profile-chart').highcharts({
        chart: {
            width: 750,
            height: 480,
            zoomType: 'xy'
        },
        title: {
            text: 'Hours Profile',
            x: -20 //center
        },
        xAxis: {
            title: {
                text: 'Hour'
            }
        },
        yAxis: {
            title: {
                text: 'Price (EUR)'
            },
            plotLines: [{
                value: 0,
                width: 1,
                color: '#808080'
            }]
        },
        tooltip: {
            valueSuffix: ' EUR'
        },
        legend: {
            layout: 'vertical',
            align: 'right',
            verticalAlign: 'middle',
            borderWidth: 0
        },
        series: []
    });

    var hourChart = $('#hour-chart').highcharts();
    var profileChart = $('#profile-chart').highcharts();

    $("#MarketCurvesFrame").on("load", function () {
        console.log("Frame loaded...");
        blockUI.stop();//then individual loadings.. change message

        $scope.MarketCurves = window.frames[0].MarketCurves;
        $scope.sensitivityChangePercentage = $scope.MarketCurves[0].Sensitivity.PercentageChange;
        $scope.$apply();

        $http.get('/Prices/LiveNordicProductionConsumption')
        .success(getSuccess)
        .error(function (status) {
            blockUI.stop();
        });

        $('[data-toggle="tooltip"]').tooltip();
    });

    $scope.changeDropDownValue = function (dropDownModelName, newValue) {
        $scope[dropDownModelName] = newValue;
        //shouldn't we put it in a watch variable...?
    }

    $scope.loadHourCurvesChart = function (hour) {
        var count = hourChart.series.length;
        for (var i = 0; i < count; i++) {
            hourChart.series[hourChart.series.length - 1].remove();
        }

        var c = $scope.MarketCurves[hour];

        var d = Enumerable.From(c.DemandCurve).Select("x => [x.Volume, x.Price]").ToArray();

        hourChart.addSeries({ name: "Demand", data: d, color: "#FF0000", step: true });

        /* Split supply by fuel*/
        var supplySeries = [];

        for (var i = 0; i < productionStackCum.length; i++) {
            var green = 128 * (1+i / productionStackCum.length);
            var color = 'rgba(0, ' + green + ', 0, 1)';

            supplySeries[i] = {
                name: "Supply - " + fuels[i],
                data: [],
                color: color,
                step: true
            };
        }
        
        for (var i = 0; i < c.SupplyCurve.length; i++) {
            var mp = c.SupplyCurve[i];
            for (var j = productionStackCum.length - 1; j >= 0; j--) {
                if (productionStackCum[j] <= mp.Volume) {
                    supplySeries[j].data.push([mp.Volume, mp.Price]);
                    break;
                }
            }
        }

        for (var i = 0; i < productionStackCum.length; i++) {
            hourChart.addSeries(supplySeries[i]);
        }
        
        hourChart.setTitle({ text: "Demand and Supply Curves for hour: " + hour });
        $scope.selectedHour = hour;
    }

    function getSuccess(productionConsumptionData) {
        /* In order of marginal cost */
        //TODO: rewrite more generic...
        var unspecified = productionConsumptionData.NotSpecifiedData[productionConsumptionData.NotSpecifiedData.length - 1].value;
        var hydro = productionConsumptionData.HydroData[productionConsumptionData.HydroData.length - 1].value;
        var nuclear = productionConsumptionData.NuclearData[productionConsumptionData.NuclearData.length - 1].value;
        var wind = productionConsumptionData.WindData[productionConsumptionData.WindData.length - 1].value;
        var thermal = productionConsumptionData.ThermalData[productionConsumptionData.ThermalData.length - 1].value;

        /* Merge unspecified with hydro... */
        var productionStack = [unspecified + hydro, nuclear, wind, thermal];
        productionStackCum = [productionStack[0]];
        for (var i = 1; i < productionStack.length; i++) {
            productionStackCum[i] = productionStackCum[i - 1] + productionStack[i];
        }

        fuels = ["Hydro", "Nuclear", "Wind", "Thermal"];
        var iconPositions = [-46, -7, -124, -85]

        for (var i = 0; i < $scope.MarketCurves.length; i++) {
            var volume = $scope.MarketCurves[i].Equilibrium.Volume;
            //just a little hack for now...
            $scope.MarketCurves[i].Fuel = fuels[0];
            $scope.MarketCurves[i].FuelIconTopPosition = iconPositions[0];
            for (var j = 0; j < productionStackCum.length - 1; j++) {
                if (productionStackCum[j] <= volume){
                    $scope.MarketCurves[i].Fuel = fuels[j + 1];
                    $scope.MarketCurves[i].FuelIconTopPosition = iconPositions[j + 1];
                }
            }
        }

        $scope.loadHourCurvesChart(0);
        loadProfilePrices();

        //TODO: refactor
        if (!$scope.$$phase) {
            $scope.$apply();
        }
    }
    
    function plotSensitivity() {
        plotBands($scope.MarketCurves, "Demand", $scope.sensitivityChangePercentage);
        plotBands($scope.MarketCurves, "Supply", $scope.sensitivityChangePercentage);
    }

    /* Muse be isolated function... or in a service??? or smt...*/
    function plotBands(curves, side, prc) {
        var sdplus = Enumerable.From(curves)
            .Select("x => [x.Hour, x.Equilibrium.Price + x.Sensitivity.PriceDelta"+side+"MinusPrc]")
            .ToArray();
        profileChart.addSeries({
            name: "Sensitivity " + side + " + " + prc + "%",
            dashStyle: "ShortDash",
            data: sdplus,
            color: "#CFCFCF",
            lineWidth: 1,
            marker: {
                enabled: false
            }
        });

        var sdminus = Enumerable.From(curves)
            .Select("x => [x.Hour, x.Equilibrium.Price + x.Sensitivity.PriceDelta" + side + "PlusPrc]")
            .ToArray();
        profileChart.addSeries({
            name: "Sensitivity " + side + " - " + prc + "x%",
            data: sdminus,
            color: "#CFCFCF",
            dashStyle: "ShortDash",
            lineWidth: 1,
            marker: {
                enabled: false
            }
        });
        blockUI.stop();//then individual loadings.. change message
    }
    
    function loadProfilePrices() {
        /* If the chart is loaded, don't reload it */
        if (profileChart.series.count > 0)
            return;

        /* Clear all */
        clearSeriesContainingName(profileChart, "");

        /* Hourly profile */
        var d = Enumerable.From($scope.MarketCurves).Select("x => [x.Hour, x.Equilibrium.Price]").ToArray();
        profileChart.addSeries({ name: "Profile", data: d, color: "#FF0000" });

        /* Sensitivity data */
        plotSensitivity();
    }

    function changeIframeUrl() {
        
        var qs = objectPropertiesToQueryString($scope,
            'sensitivityChangePercentage', 'date', 'EquilibriumAlgorithm', 'EquilibriumFill');
        blockUI.start();
        $scope.CurvesIframeUrl = "/Plotly/SpotMarketCurvesSurface?" + qs;
    }

    function dateChanged(newval, oldval) {
        var sameDay = areSameDates(newval, oldval);
        if (sameDay) return;

        changeIframeUrl();
    }

    function sensitivityChangePercentageChanged(newval, oldval) {
        if (newval == oldval || newval > 100 || newval < 0 || oldval == undefined)
            return;

        changeIframeUrl();

        //$blockUI.start();
        //here we should post all the model, for it to work... so?...
        /*
        $http.post('/Simulations/SpotPriceConfidence', data)
            .success(postSuccess)
            .error(function (status) {

            });
        */
    }

    function dropDownChanged(newval, oldval) {
        if (newval == undefined || oldval == undefined || newval == oldval)
            return;
        changeIframeUrl();
    }

    //change to register form watches..?
    $scope.$watch('date', dateChanged);
    $scope.$watch('EquilibriumAlgorithm', dropDownChanged);
    $scope.$watch('EquilibriumFill', dropDownChanged);
    $scope.$watch('sensitivityChangePercentage', sensitivityChangePercentageChanged);

    //this will start downloading
    changeIframeUrl();
});