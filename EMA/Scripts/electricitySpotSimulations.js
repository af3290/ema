emaDemos.controller('SpotController', function ($scope, $http) {
    $scope.volatility = 0.2;
    $scope.reversionRate = 25;
    $scope.confidence = 0.95;
    $scope.simulationsCount = 1;
    $scope.simulationSeries = [];

    $scope.$http = $http;

    $scope.postUrl = {
        "Data": "/Prices/HistoricalSystemPrice?refresh=false&resolution=5",
    };

    $scope.onChangedPostResponse = { "Data": dataSuccess };

    //prepare variables... put somewhere...
    $scope.httpPostParameters = [];
    $scope.objectWatchers = {};

    registerParameter($scope, 'volatility', 0.2);
    registerParameter($scope, 'forwardInterpolation', $scope.ForwardInterpolations[0], "Spikes");

    var chart = $('#container').highcharts('StockChart', {
        tooltip: {
            valueDecimals: 4
        },
        rangeSelector: {
            selected: 1
        },
        title: {
            text: 'Nordic Forward Contracts... :D'
        },
        xAxis: {
            ordinal: false,
            type: 'datetime'
            //,tickInterval: 86400000
        },
        series: []
    });

    var historicalStartDate, forwardEndDate;

    function dataSuccess(data) {
        var priceData = [];
        for (var i = 0; i < data.length; i++) {
            //try using linq.js => YES!
            var dt = Date.parse(data[i].DateTime);
            priceData[i] = [dt, data[i].Value];
        }

        var serie = {
            name: "Historical Price",
            color: '#CFCFCF',
            data: priceData,
            tooltip: {
                valueDecimals: 4
            },
            zIndex: 9
        };

        chart.addSeries(serie);
        
        //doPageParametersPost($scope, "Forecast");
        historicalStartDate = Date.parse(data[0].DateTime);
    }

    //sorta global variables...
    window.chart = chart = $('#container').highcharts(); //
    window.forwardLevels = [];
    window.timeSteps = [];
    window.startingDateForwardCurve;
    var timeStep = 0; //hourly, daily, monthly, quarterly, yearly... 

    $.getJSON('/Prices/ForwardCurve?region=nordic&nonOverlapping=true', function (data) {
        startingDateForwardCurve = Date.parse(data[0].Begin);

        for (var i = 0; i < data.length; i++) {
            var fwd = data[i];

            var price = fwd.FixPrice;
            var begin = Date.parse(fwd.Begin);
            forwardEndDate = Date.parse(fwd.End);
            //this is daily, timeStep = 86400000
            var diff = Math.ceil((forwardEndDate - begin) / 86400000);

            var priceData = [];
            forwardLevels.push(price);
            timeSteps.push(diff + 1);

            //we must replicate the price daily, so the server won't have that high of a burden
            for (var j = 0; j <= diff; j++) {
                priceData[j] = [begin + j * 86400000, price];
            }

            var serie = {
                name: fwd.Contract,
                data: priceData
            };

            chart.addSeries(serie);            
        }

        

        $("#container").trigger("ForwardContractsReady");
    });

    $scope.simulate = function() {
        var data = {
            timeStepsInLevels: timeSteps,
            priceLevels: forwardLevels,
            timeStep: 1 / 360,
            reversionRate: $scope.reversionRate,
            volatility: $scope.volatility,
            numberOfSimulations: 1
        };

        function postSuccess(simulations) {
            for (var i = 0; i < simulations.length; i++) {
                var simulation = simulations[i];
                var simulationData = [];
                for (var j = 0; j < simulation.length; j++) {
                    simulationData[j] = [startingDateForwardCurve + j * TICKS_IN_HOUR, simulation[j]];
                }
                
                var serie = {
                    name: "Simulated Path " + ($scope.simulationsCount++),
                    color: "#FF0000",
                    dashStyle: "ShortDot",
                    data: simulationData
                };

                chart.addSeries(serie);
            }
        }
        
        $http.post('/Simulations/SpotPriceSimulation', data)
            .success(postSuccess)
            .error(function(status) {
                
            });
    }

    $scope.clear = function () {
        for (var i = 0; i < $scope.simulationsCount; i++) {
            chart.series[chart.series.length - 1].remove();
        }
        $scope.simulationsCount = 0;
    }

    function confidenceChanged(newval, oldval) {
        //shouldn't be called, but angularjs first time when it loads it calls it with them being equal
        if (newval === oldval)
            return;

        if (newval < 0.5 || newval > 1) {
            //error...
            return;
        }

        var data = {
            timeStepsInLevels: timeSteps,
            priceLevels: forwardLevels,
            timeStep: 1 / 360,
            reversionRate: $scope.reversionRate,
            volatility: $scope.volatility,
            alpha: newval
        };

        function postSuccess(confidencesObj) {
            clearSeriesContainingName(chart, "Confidence");

            var confidences = confidencesObj.ConfidenceIntervals;
            var fxxx = confidencesObj.ForwardInterpolation;

            for (var i = 0; i < confidences.length; i++) {
                var confidenceSerie = confidences[i];
                var confidenceValues = [];
                for (var j = 0; j < confidenceSerie.length; j++) {
                    confidenceValues[j] = [startingDateForwardCurve + j * 86400000, confidenceSerie[j]];
                }

                var serie = {
                    name: "Confidence " + ((i===0)?"Upper":"Lower"),
                    color: "#FF0000",
                    type: 'spline',
                    dashStyle: "Solid",
                    data: confidenceValues
                };

                chart.addSeries(serie);
            }

            //should be in forwards... etc...
            forwardEndDate = confidenceValues[confidenceValues.length - 1][0];

            chart.xAxis[0].setExtremes(historicalStartDate, forwardEndDate);
        }

        $http.post('/Simulations/SpotPriceConfidence', data)
            .success(postSuccess)
            .error(function (status) {

            });

    }

    $("#container").on("ForwardContractsReady", function () { confidenceChanged($scope.confidence) });

    $scope.$watch('confidence', confidenceChanged);

    /* Start all */
    doPageParametersPost($scope, "Data");
});