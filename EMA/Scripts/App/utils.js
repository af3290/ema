///When passed with empty seriesName "", clears all...
function clearSeriesContainingName(highchart, seriesName) {
    if (highchart == undefined || highchart.series == undefined)
        return;

    for (var i = 0; i < highchart.series.length; i++) {
        if (highchart.series[i].name.indexOf(seriesName) !== -1)
            highchart.series[i].remove();
    }
}

function addConfidenceBands(highchart, confidenceBands) {

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
        console.error("Must have frame Id, but will use current...");
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

//generic method
function registerParameter($scope, param, value) {
    $scope[param] = value;
        
    $scope.httpPostParameters.push(param);
        
    if (value.constructor == Date) {
        $scope.$watch(param, function (newval, oldval) {
            var sameDay = areSameDates(newval, oldval);
            if (sameDay) return;

            doPageParametersPost($scope);
        });
    } else if (value.constructor == Number || value.constructor == String) {
        $scope.$watch(param, function (newval, oldval) {
            if (newval == oldval || newval == undefined || oldval == undefined)
            return;

            doPageParametersPost($scope);
        });
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
//and also watches it...
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