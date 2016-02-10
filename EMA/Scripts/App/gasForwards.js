emaDemos.controller('GasForwardCurvesController', function ($scope, $http) {
    //abstract charts to a projects called .WebUtils... all forecasting and graphs must be there...
    //and maybe used as components... GREAT IDEA!
    $scope.$http = $http;

    $scope.postUrl = {
        "Data": "/Prices/GasFutures",
        "Simulations": "/Simulations/ForwardCurveSimulations",
        "Confidence": "/Simulations/ForwardCurveConfidence"
    };
    $scope.onChangedPostResponse = {
        "Data": postSuccess,
        "Simulations": simulationPostSuccess,
        "Confidence": confidencePostSuccess
    };
    
    $scope.currentFrameId = "Simulations";

    //prepare variables... put somewhere...
    $scope.httpPostParameters = [];
    $scope.objectWatchers = {};

    registerParameter($scope, 'date', new Date(2014, 4, 15), 'Data');

    //bounds...
    $scope.dateMin = new Date(2005, 0, 1);
    $scope.dateMax = new Date(2015, 6, 1);

    //simulation parameters, is it ok to store them alltogether, no, use frameId
    registerParameter($scope, 'curve', "Natural-Gas-Futures-NYMEX");
    registerParameter($scope, 'forwardSteps', 12);
    registerParameter($scope, 'timeHorizon', 30);
    registerParameter($scope, 'numberSimulations', 5);
    registerParameter($scope, 'confidence', 0.95, "Confidence");
    registerParameter($scope, 'method', "PCA");
    
    //todo: server constants...
    $scope.Currency = "€";
    
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
    var currentDates = [];

    function postSuccess(data) {        
        clearSeriesContainingName(chartCurrent, "Forward");
        clearSeriesContainingName(chartCurrent, "Simulation");

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
                if (areSameDates($scope.date, dt)){
                    currentForwardCurve[j] = [mat, futuresSeries[i].Value];
                    currentDates[j] = mat;
                }
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
           
        chartCurrent.addSeries(serie);

        var rangeStartDate = Date.parse(data[0].Maturity);
        var rangeEndDate = Date.parse(data[data.length - 1].Maturity);
        chartCurrent.xAxis[0].setExtremes(rangeStartDate, rangeEndDate);

        doPageParametersPost($scope, "Simulations");
        doPageParametersPost($scope, "Confidence");
    }
   
    function simulationPostSuccess(data) {
        clearSeriesContainingName(chartCurrent, "Simulation");

        for (var j = 0; j < data[0].length; j++) {

            for (var i = 0; i < data.length; i++) {
                currentForwardCurve[i][1] = data[i][j];
            }

            var serie = {
                name: "Simulation No " + j,
                color: '#000099',
                dashStyle: "ShortDot",
                lineWidth: 0.75,
                data: currentForwardCurve,
                tooltip: {
                    valueDecimals: 4
                }
            };

            chartCurrent.addSeries(serie);
        }
    }
    
    function confidencePostSuccess(data) {
        addConfidenceBands(chartCurrent, data, currentDates)
    }

    /* Start all by faking a parameter change... */
    doPageParametersPost($scope, "Data");
});