emaDemos.controller('ForecastController', function ($scope, $http, blockUI) {
    //abstract charts to a projects called .WebUtils... all forecasting and graphs must be there...
    //and maybe used as components... GREAT IDEA!

    $scope.date = new Date(2015, 7, 3); //js months start at 0..
    angular.extend($scope, SERVER_CONSTANTS);
    $scope.forecastMethod = $scope.ForecastMethods[0];
    $scope.timeHorizon = $scope.TimeHorizons[0];
    $scope.spikesPreprocessMethod = $scope.SpikesPreprocessMethods[0];
    $scope.confidence = 95;
    $scope.exogenousVariables = {};//the selected values...
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
    
    /* Private methods */
    function getSuccess(dataObj) {
        clearSeriesContainingName(chart, "Forecast");
        clearSeriesContainingName(chart, "Confidence");

        var forecast = dataObj.Result.Forecast;
        
        var dt = Date.parse($scope.date);
        var data = [];
        for (var j = 0; j < forecast.length; j++) {
            //TODO: fix the +2 error, GMT? or why?
            data[j] = [dt + (j+2) * 3600000, forecast[j]];
        }
                
        var serie = {
            name: "Forecast ",
            color: "#FF0000",
            dashStyle: "ShortDot",
            data: data
        };

        chart.addSeries(serie);

        var confidences = dataObj.Result.Confidence;

        for (var i = 0; i < confidences.length; i++) {
            var confidenceSerie = confidences[i];
            var confidenceValues = [];
            for (var j = 0; j < confidenceSerie.length; j++) {
                confidenceValues[j] = [dt + (j + 2) * 3600000, confidenceSerie[j]];
            }

            var serie = {
                name: "Confidence " + ((i === 0) ? "Upper" : "Lower"),
                color: "#FF0000",
                type: 'spline',
                dashStyle: "Solid",
                data: confidenceValues
            };

            chart.addSeries(serie);
        }

        var thisDate = new Date($scope.date);
        thisDate.setTime(thisDate.getTime() + (2 * 60 * 60 * 1000)); //UTC bullshit?...
        var rangeStartDate = Date.parse(thisDate);
        var rangeEndDate = thisDate.setDate(thisDate.getDate() + dataObj.DaysAhead);
        //todo: fix UTC...
        //rangeEndDate.setTime(rangeEndDate.getTime() + (2 * 60 * 60 * 1000));
        chart.xAxis[0].setExtremes(rangeStartDate, rangeEndDate);

        angular.extend($scope, dataObj.BaseFit); //or merge the whole data object...?
        blockUI.stop();
    }
    
    function loadForecast() {
        var data = objectPropertiesToObj($scope, 'date',
            'forecastMethod', 'timeHorizon', 'confidence', 'exogenousVariables'); //how? , 'exogenousVariables'
        blockUI.start();
        $http.post('/Prices/Forecast', data) //default parameters?...
        .success(getSuccess)
        .error(function (status) {

        });
        var qs = "";
        
        $scope.ForecastSurfaceIframeUrl = "/Plotly/ProbabilitySurface?series=ElsSystem&bins=6&period=24&distributionType=empirical" + qs;
    }

    function loadHistoricalData() {
        $http.get('/Prices/HistoricalSystemPrice?refresh=false&resolution=5').success(function (data) {
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
                }
            };

            chart.addSeries(serie);
                
            loadForecast();
        });
    }
            
    function varChanged(newval, oldval) {
        if (newval == oldval || newval == undefined || oldval == undefined)
            return;

        loadForecast();        
    }

    $scope.changeDropDownValue = function (dropdown, value) {
        $scope[dropdown] = value;
    }

    //come up with an idea to have them global... yes...
    function dateChanged(newval, oldval) {
        var sameDay = areSameDates(newval, oldval);
        if (sameDay) return;

        loadForecast();
    }

    $scope.$watch('date', dateChanged);
    $scope.$watch('forecastMethod', varChanged);
    $scope.$watch('timeHorizon', varChanged);
    $scope.$watch('confidence', varChanged);
    $scope.$watch('exogenousVariables', varChanged);

    /* Start all */
    loadHistoricalData()
});