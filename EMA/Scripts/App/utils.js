///When passed with empty seriesName "", clears all...
function clearSeriesContainingName(highchart, seriesName) {
    if (highchart == undefined || highchart.series == undefined)
        return;

    //local copy of length, since the series array will resize...
    var len = highchart.series.length;

    //remove decreasingly, since the series array will resize as items are removed and that results in failing to remove some...
    for (var i = len-1; i >= 0; i--) {
        if (highchart.series[i] != undefined && highchart.series[i].name.indexOf(seriesName) !== -1)
            highchart.series[i].remove();
    }
}

function addConfidenceBands(chart, dataObj, currentDates) {
    clearSeriesContainingName(chart, "Confidence");

    var confidences = dataObj.Confidence;
    var confidenceBand = [];
    var bandCount = confidences.length / 2;

    //for future reference
    var dt = undefined;

    //add confidence values...
    for (var i = 0; i < confidences.length; i++) {
        var confidenceSerie = confidences[i];
        var confidenceValues = [];

        //first upper, then lower
        for (var j = 0; j < confidenceSerie.length; j++) {
            if (dt == undefined) {
                var dVal = currentDates[j];
            } else {
                var dVal = dt + (j + 2) * TICKS_IN_HOUR;
            }            

            var cVal = confidenceSerie[j];

            confidenceValues[j] = [dVal, cVal];

            //start doing the bands when lower intervals are processed...
            if (i >= bandCount) {
                var subi = confidences.length - i - 1;
                confidenceBand[subi][j] = [dVal, confidences[subi][j], cVal]
            } else {
                //initialize
                confidenceBand[i] = [];
            }
        }

        var serie = {
            name: "Confidence " + ((i < bandCount) ? "Upper" : "Lower")
                + prcFrmt(dataObj.ConfidenceLevels[i % bandCount]) + ' %',
            color: "#FF0000",
            type: 'line',
            dashStyle: "ShortDot",
            data: confidenceValues,
            zIndex: 9,
            enableMouseTracking: false
        };

        //don't add individual series for now...
        //chart.addSeries(serie);
    }

    //starts from narrower to winder
    for (var i = 0; i < bandCount; i++) {
        /* Add confidence bands areas */
        var cofidenceBandArea = {
            name: 'Confidence Band ' + prcFrmt(dataObj.ConfidenceLevels[(bandCount - i - 1)]) + ' %',
            data: confidenceBand[i],
            type: 'arearange',
            lineWidth: 0,
            linkedTo: ':previous',
            color: '#FF0000',
            fillOpacity: 0.05 + (bandCount - i) * 0.05,
            zIndex: (bandCount - i)
        };

        chart.addSeries(cofidenceBandArea);
    }
}

/* Misccccs */
function prcFrmt(prc) {
    return (prc * 100).toFixed(2);
}

function objectPropertiesToObj() {
    $scope = arguments[0];

    if (arguments[1] != undefined && arguments[1].constructor == Array) {
        var argss = arguments[1];
        arguments = [null]; //1 start index...
        arguments = arguments.concat(argss);
    }

    var res = {};

    for (var i = 1; i < arguments.length; i++) {
        var val = $scope[arguments[i]];
        if (val.constructor != undefined && val.constructor == Object)
            val = JSON.stringify(val);
        res[arguments[i]] = val;
    }
    return res;
}

function objectPropertiesToQueryString() {
    $scope = arguments[0];

    if (arguments[1] != undefined && arguments[1].constructor == Array)
        arguments = arguments[1];

    var res = {};
    for (var i = 1; i < arguments.length; i++) {
        res[arguments[i]] = $scope[arguments[i]];
    }
    return toQueryString(res);
}

function scopeArgs() {
    $scope = arguments[0];
    var res = {};
    for (var i = 1; i < arguments.length; i++) {
        res[arguments[i]] = $scope[arguments[i]];
    }
    return res;
}

function toQueryString(obj) {
    var str = [];
    for (var p in obj)
        if (obj.hasOwnProperty(p)) {
            var val = encodeURIComponent(obj[p]);
            if(obj[p].constructor != undefined && obj[p].constructor == Date)
                val = obj[p].toISOString();
            //if (obj[p].constructor != undefined && obj[p].constructor == Object)
            //    val = obj[p].toISOString();
            str.push(encodeURIComponent(p) + "=" + val);
        }
    return str.join("&");
}

function areSameDates(d1, d2) {
    var newval = d1, oldval = d2;

    if (newval == undefined && oldval == undefined)
        return true;

    if (newval == undefined || oldval == undefined)
        return false;

    if (newval.constructor == Number)
        newval = new Date(newval);
    if (oldval.constructor == Number)
        oldval = new Date(oldval);

    return newval.getDate() == oldval.getDate()
        && newval.getMonth() == oldval.getMonth()
        && newval.getYear() == oldval.getYear();
}

//partial parameters posts via frameId
//if null posts all data...
function doPageParametersPost($scope, frameId) {
    if (frameId == undefined) {
        console.warn("Must have frame Id, but will use current...");
        frameId = $scope.currentFrameId;
    }
    if ($scope.postUrl[frameId] == undefined)
        throw "No frame url"
    if ($scope.onChangedPostResponse[frameId] == undefined)
        throw "No frame callback"

    var data = objectPropertiesToObj($scope, $scope.httpPostParameters);

    $scope.$http.post($scope.postUrl[frameId], data) //default parameters?...
        .success($scope.onChangedPostResponse[frameId])
        .error(function (status) {
            alert("Post failed...");
        });
}

//generic method to attach parameter by name and value to the scope
//so that when changed those belonging to a particular frame will pe posted to the according url
function registerParameter($scope, param, value, frameId) {
    $scope[param] = value;
        
    $scope.httpPostParameters.push(param);
        
    if (value.constructor == Date) {
        $scope.$watch(param, function (newval, oldval) {
            var sameDay = areSameDates(newval, oldval);
            if (sameDay) return;

            doPageParametersPost($scope, frameId);
        });
    } else if (value.constructor == Number || value.constructor == String) {
        $scope.$watch(param, function (newval, oldval) {
            if (newval == oldval || newval == undefined || oldval == undefined)
            return;

            doPageParametersPost($scope, frameId);
        });
    } else if (value.constructor == Object) {
        $scope.$watch(param, function (newval, oldval) {
            if (newval == oldval || newval == undefined || oldval == undefined)
                return;

            doPageParametersPost($scope, frameId);
        }, true);
    }

    //set global scope variables... TODO: find a better place to set them...
    $scope.extendWith = extendWith;
    $scope.extendWithAndListen = extendWithAndListen;
    angular.extend($scope, SERVER_CONSTANTS);

    $scope.changeDropDownValue = function (dropdown, value) {
        $scope[dropdown] = value;
    }
}

//Extends the angular scope with a child provided as a child from given object found by the given name
function extendWith(name, object) {
    this[name] = {};
    var obj = object[name];

    angular.extend(this[name], obj);
}

//Extends the angular scope with a child provided as a child from given object found by the given name
//and also watches it... in TODO: it should have only primitive properties, arrays will be flattened, on changes they will be converted back...
function extendWithAndListen(name, object) {
    var oldObj = this[name];

    //if it already exists under the same structure, don't readd, do it only when we have a different object structure
    if (oldObj != undefined && angular.equals(Object.keys(oldObj), Object.keys(object)))
        return;

    this[name] = {};
    var obj = object[name];
    
    angular.extend(this[name], obj);

    console.log("Watching child object, " + name);

    $scope = this;

    //deregister listener
    if ($scope.objectWatchers[name] != undefined && $scope.objectWatchers[name].constructor == Function)
        $scope.objectWatchers[name]();

    //watch a whole object
    var listener = this.$watch(name, function (newval, oldval) {
        if (newval == oldval || newval == undefined || oldval == undefined || angular.equals(newval, oldval))
            return;

        console.log("Child object changed... ");

        doPageParametersPost($scope);
    }, true);

    $scope.objectWatchers[name] = listener;
    
    if ($scope.httpPostParameters.indexOf(name) == -1)
        $scope.httpPostParameters.push(name);
}