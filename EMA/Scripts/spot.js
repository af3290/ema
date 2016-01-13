emaDemos.controller('SpotController', function ($scope, $http) {
    $scope.volatility = 0.2;
    $scope.reversionRate = 25;
    $scope.confidence = 0.95;
    $scope.simulationsCount = 0;
    $scope.simulationSeries = [];

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
                    simulationData[j] = [startingDateForwardCurve + j * 86400000, simulation[j]];
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

        function postSuccess(confidences) {
            clearSeriesContainingName("Confidence");
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
        }

        $http.post('/Simulations/SpotPriceConfidence', data)
            .success(postSuccess)
            .error(function (status) {

            });

    }

    $("#container").on("ForwardContractsReady", function () { confidenceChanged($scope.confidence) });

    $scope.$watch('confidence', confidenceChanged);
});