emaDemos.controller('GasForwardCurvesController', function ($scope, $http) {
    //abstract charts to a projects called .WebUtils... all forecasting and graphs must be there...
    //and maybe used as components... GREAT IDEA!
    $scope.$http = $http;

    $scope.postUrl = { "Data": "/Prices/GasFutures", "Simulations": "/Simulations/ForwardCurveSimulations" };
    $scope.onChangedPostResponse = { "Data": postSuccess, "Simulations": simulationPostSuccess };
    
    registerParameter($scope, 'date', new Date(2014, 4, 15), postSuccess);

    //bounds...
    $scope.dateMin = new Date(2005, 0, 1);
    $scope.dateMax = new Date(2015, 6, 1);

    //have them here so that they could also be edited... but always set up to show forward 12 months!!!
    registerParameter($scope, 'afterMaturity', new Date(2014, 5, 1));
    registerParameter($scope, 'beforeMaturity', new Date(2015, 4, 1));

    //simulation parameters, is it ok to store them alltogether, no, use frameId
    registerParameter($scope, 'curve', "Natural-Gas-Futures-NYMEX");
    registerParameter($scope, 'forwardSteps', 12);
    registerParameter($scope, 'timeHorizon', 9);
    registerParameter($scope, 'numberSimulations', 9);
    registerParameter($scope, 'method', "PCA");
    
    //todo: server constants...
    $scope.Currency = "€";

    var chart = $('#container').highcharts('StockChart', {
        tooltip: {
            valueDecimals: 4
        },
        rangeSelector: {
            selected: 1
        },
        title: {
            text: 'Forecast'
        },
        xAxis: {
            ordinal: false,
            type: 'datetime'
            //,tickInterval: 86400000
        },
        series: []
    });

    window.chart = chart = $('#container').highcharts();

    var chartCurrent = $('#container-current').highcharts('StockChart', {
        tooltip: {
            valueDecimals: 4
        },
        rangeSelector: {
            selected: 1
        },
        title: {
            text: 'Forecast'
        },
        xAxis: {
            ordinal: false,
            type: 'datetime'
            //,tickInterval: 86400000
        },
        series: []
    });

    window.chartCurrent = chartCurrent = $('#container-current').highcharts();
    
    var currentForwardCurve = []; //to be used around...

    function postSuccess(data) {        
        clearSeriesContainingName(chartCurrent, "Futures");

        //all forward curves added
        for (var j = 0; j < data.length; j++) {
            var futuresSeries = data[j].Prices;
            var mat = Date.parse(data[j].Maturity);
            var priceData = [];
            for (var i = 0; i < futuresSeries.length; i++) {
                //try using linq.js => YES!
                var dt = Date.parse(futuresSeries[i].DateTime);
                priceData[i] = [dt, futuresSeries[i].Value];

                //value of futures at the given date                                        
                if (areSameDates($scope.date, dt))
                    currentForwardCurve[j] = [mat, futuresSeries[i].Value];
            }

            //where should the be? server side? client side?...
            var sName = data[j].Name.split("-");

            var serie = {
                name: "Futures Price " + sName[3] + "-" + sName[4],
                color: '#CFCFCF',
                data: priceData,
                tooltip: {
                    valueDecimals: 4
                }
            };

            chart.addSeries(serie);
        }            
                
        //current curve as well.. for simulations to display...
        var serie = {
            name: "Forward Curve " + $scope.date.toISOString(),
            color: '#CFCFCF',
            data: currentForwardCurve,
            tooltip: {
                valueDecimals: 4
            }
        };

        clearSeriesContainingName(chartCurrent, "");
        chartCurrent.addSeries(serie);
    }
   
    function simulationPostSuccess(data) {

        for (var j = 0; j < data[0].length; j++) {

            for (var i = 0; i < data.length; i++) {
                currentForwardCurve[i][1] = data[i][j];
            }

            var serie = {
                name: "Simulation No " + j,
                color: '#FF0000',
                data: currentForwardCurve,
                tooltip: {
                    valueDecimals: 4
                }
            };

            chartCurrent.addSeries(serie);
        }
    }

    $scope.simulate = function () {
        doPageParametersPost($scope, "Simulations");
    }
        
    /* Start all by faking a parameter change... */
    doPageParametersPost($scope, "Data");
});