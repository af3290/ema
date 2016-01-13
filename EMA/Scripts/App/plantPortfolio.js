emaDemos.controller('PlantPortfolioController', function ($scope, $http) {
    //abstract charts to a projects called .WebUtils... all forecasting and graphs must be there...
    //and maybe used as components... GREAT IDEA!

    $scope.date = new Date(2015, 7, 3); //js months start at 0..
   
    $scope.Currency = "€";

    var chart = $('#container-var').highcharts({
        chart: {          
            type: 'column'
        },
        tooltip: {
            valueDecimals: 4
        },
        title: {
            text: 'VaR'
        },
        series: [],
        xAxis: {
            
            minPadding: 0,
            maxPadding: 0
        }
    });

    window.chart = chart = $('#container-var').highcharts();

    /* Private methods */

    function getSuccess(dataObj) {
        
    }

    function loadForecast() {
        var th = $scope.TimeHorizons.indexOf($scope.timeHorizon);

        $http.get('/Prices/Forecast?priceSeries=Elspot-SystemPrice&forecastHorizon=' + th
            + '&method=Naive&dateTime='
            + $scope.date.toISOString()) //default parameters?...
        .success(getSuccess)
        .error(function (status) {

        });
    }

    function loadHistoricalData() {
        var priceData = [];
        for (var i = 0; i < 100; i++) {
            priceData[i] = -i*(i-99);
        }

        var serie = {
            name: "Cash Flow",
            color: '#CFCFCF',
            data: priceData,
            tooltip: {
                valueDecimals: 4
            }
        };

        chart.addSeries(serie);

        //VaR lines..
        chart.xAxis[0].addPlotLine({
            color: 'red', // Color value
            value: 3, // Value of where the line will appear
            width: 2 // Width of the line
        });

        chart.xAxis[0].addPlotLine({
            label: {
                text: "90% VaR",
                verticalAlign: 'top',
                textAlign: 'center'
            },
            color: 'yellow', // Color value
            value: 10, // Value of where the line will appear
            width: 2 // Width of the line
        });

        //add different colors, make it work really nice
        //YES, make it cool...
    }

    function varChanged(newval, oldval) {
        if (newval == oldval || newval == undefined || oldval == undefined)
            return;

        loadForecast();
    }

    /* Methods of controller */
    $scope.changeDropdown = function (dropdown, value) {
        $scope[dropdown] = value;
    }

    $scope.$watch('forecastMethod', varChanged);
    $scope.$watch('timeHorizon', varChanged);

    /* Start all */
    loadHistoricalData()
});