//---------------------------- TRACKING CUSTOMER IP,OS, BROWSER ----------------------------//
(function (window) {
    {
        var unknown = '-';

        // screen
        var screenSize = '';
        if (screen.width) {
            width = (screen.width) ? screen.width : '';
            height = (screen.height) ? screen.height : '';
            screenSize += '' + width + " x " + height;
        }

        // browser
        var url = window.location.href;
        var nVer = navigator.appVersion;
        var nAgt = navigator.userAgent;
        var browser = navigator.appName;
        var version = '' + parseFloat(navigator.appVersion);
        var majorVersion = parseInt(navigator.appVersion, 10);
        var nameOffset, verOffset, ix;

        // Opera
        if ((verOffset = nAgt.indexOf('Opera')) != -1) {
            browser = 'Opera';
            version = nAgt.substring(verOffset + 6);
            if ((verOffset = nAgt.indexOf('Version')) != -1) {
                version = nAgt.substring(verOffset + 8);
            }
        }
        // Opera Next
        if ((verOffset = nAgt.indexOf('OPR')) != -1) {
            browser = 'Opera';
            version = nAgt.substring(verOffset + 4);
        }
            // Edge
        else if ((verOffset = nAgt.indexOf('Edge')) != -1) {
            browser = 'Microsoft Edge';
            version = nAgt.substring(verOffset + 5);
        }
            // MSIE
        else if ((verOffset = nAgt.indexOf('MSIE')) != -1) {
            browser = 'Microsoft Internet Explorer';
            version = nAgt.substring(verOffset + 5);
        }
            // Chrome
        else if ((verOffset = nAgt.indexOf('Chrome')) != -1) {
            browser = 'Chrome';
            version = nAgt.substring(verOffset + 7);
        }
            // Safari
        else if ((verOffset = nAgt.indexOf('Safari')) != -1) {
            browser = 'Safari';
            version = nAgt.substring(verOffset + 7);
            if ((verOffset = nAgt.indexOf('Version')) != -1) {
                version = nAgt.substring(verOffset + 8);
            }
        }
            // Firefox
        else if ((verOffset = nAgt.indexOf('Firefox')) != -1) {
            browser = 'Firefox';
            version = nAgt.substring(verOffset + 8);
        }
            // MSIE 11+
        else if (nAgt.indexOf('Trident/') != -1) {
            browser = 'Microsoft Internet Explorer';
            version = nAgt.substring(nAgt.indexOf('rv:') + 3);
        }
            // Other browsers
        else if ((nameOffset = nAgt.lastIndexOf(' ') + 1) < (verOffset = nAgt.lastIndexOf('/'))) {
            browser = nAgt.substring(nameOffset, verOffset);
            version = nAgt.substring(verOffset + 1);
            if (browser.toLowerCase() == browser.toUpperCase()) {
                browser = navigator.appName;
            }
        }
        // trim the version string
        if ((ix = version.indexOf(';')) != -1) version = version.substring(0, ix);
        if ((ix = version.indexOf(' ')) != -1) version = version.substring(0, ix);
        if ((ix = version.indexOf(')')) != -1) version = version.substring(0, ix);

        majorVersion = parseInt('' + version, 10);
        if (isNaN(majorVersion)) {
            version = '' + parseFloat(navigator.appVersion);
            majorVersion = parseInt(navigator.appVersion, 10);
        }

        // mobile version
        var mobile = /Mobile|mini|Fennec|Android|iP(ad|od|hone)/.test(nVer);

        // cookie
        var cookieEnabled = (navigator.cookieEnabled) ? true : false;

        if (typeof navigator.cookieEnabled == 'undefined' && !cookieEnabled) {
            document.cookie = 'testcookie';
            cookieEnabled = (document.cookie.indexOf('testcookie') != -1) ? true : false;
        }

        // system
        var os = unknown;
        var clientStrings = [
            { s: 'Windows 10', r: /(Windows 10.0|Windows NT 10.0)/ },
            { s: 'Windows 8.1', r: /(Windows 8.1|Windows NT 6.3)/ },
            { s: 'Windows 8', r: /(Windows 8|Windows NT 6.2)/ },
            { s: 'Windows 7', r: /(Windows 7|Windows NT 6.1)/ },
            { s: 'Windows Vista', r: /Windows NT 6.0/ },
            { s: 'Windows Server 2003', r: /Windows NT 5.2/ },
            { s: 'Windows XP', r: /(Windows NT 5.1|Windows XP)/ },
            { s: 'Windows 2000', r: /(Windows NT 5.0|Windows 2000)/ },
            { s: 'Windows ME', r: /(Win 9x 4.90|Windows ME)/ },
            { s: 'Windows 98', r: /(Windows 98|Win98)/ },
            { s: 'Windows 95', r: /(Windows 95|Win95|Windows_95)/ },
            { s: 'Windows NT 4.0', r: /(Windows NT 4.0|WinNT4.0|WinNT|Windows NT)/ },
            { s: 'Windows CE', r: /Windows CE/ },
            { s: 'Windows 3.11', r: /Win16/ },
            { s: 'Android', r: /Android/ },
            { s: 'Open BSD', r: /OpenBSD/ },
            { s: 'Sun OS', r: /SunOS/ },
            { s: 'Chrome OS', r: /CrOS/ },
            { s: 'Linux', r: /(Linux|X11(?!.*CrOS))/ },
            { s: 'iOS', r: /(iPhone|iPad|iPod)/ },
            { s: 'Mac OS X', r: /Mac OS X/ },
            { s: 'Mac OS', r: /(MacPPC|MacIntel|Mac_PowerPC|Macintosh)/ },
            { s: 'QNX', r: /QNX/ },
            { s: 'UNIX', r: /UNIX/ },
            { s: 'BeOS', r: /BeOS/ },
            { s: 'OS/2', r: /OS\/2/ },
            { s: 'Search Bot', r: /(nuhk|Googlebot|Yammybot|Openbot|Slurp|MSNBot|Ask Jeeves\/Teoma|ia_archiver)/ }
        ];
        for (var id in clientStrings) {
            var cs = clientStrings[id];
            if (cs.r.test(nAgt)) {
                os = cs.s;
                break;
            }
        }

        var osVersion = unknown;

        if (/Windows/.test(os)) {
            osVersion = /Windows (.*)/.exec(os)[1];
            os = 'Windows';
        }

        switch (os) {
            case 'Mac OS X':
                osVersion = /Mac OS X (10[\.\_\d]+)/.exec(nAgt)[1];
                break;

            case 'Android':
                osVersion = /Android ([\.\_\d]+)/.exec(nAgt)[1];
                break;

            case 'iOS':
                osVersion = /OS (\d+)_(\d+)_?(\d+)?/.exec(nVer);
                osVersion = osVersion[1] + '.' + osVersion[2] + '.' + (osVersion[3] | 0);
                break;
        }

        // flash (you'll need to include swfobject)
        /* script src="//ajax.googleapis.com/ajax/libs/swfobject/2.2/swfobject.js" */
        var flashVersion = 'no check';
        if (typeof swfobject != 'undefined') {
            var fv = swfobject.getFlashPlayerVersion();
            if (fv.major > 0) {
                flashVersion = fv.major + '.' + fv.minor + ' r' + fv.release;
            }
            else {
                flashVersion = unknown;
            }
        }
    }
    window.jscd = {
        screen: screenSize,
        browser: browser,
        browserVersion: version,
        browserMajorVersion: majorVersion,
        mobile: mobile,
        os: os,
        osVersion: osVersion,
        cookies: cookieEnabled,
        flashVersion: flashVersion
    };
    // ip device access
    window.ipInfo = {
        ip: '',
        city: '',
        region: '',
        latitude: '',
        longtitude: ''
    };
    var getIP = function () {
        var temp = null;
        $.ajax({
            type: 'GET',
            async: false,//muốn pass data ra ngoài biến nên có asynce
            global: false,
            url: 'https://ipinfo.io?token=d4b73a8d673d31',
            success: function (data) {
                tmp = data;
            }
        });
        return tmp;
    }();
    if (getIP != null) {
        ipInfo.ip = getIP.ip;
        ipInfo.city = getIP.city;
        ipInfo.region = getIP.region;
        ipInfo.latitude = getIP.loc.split(',')[0];
        ipInfo.longtitude = getIP.loc.split(',')[1];
    }

}(this));
console.log(
    'OS: ' + jscd.os + ' ' + jscd.osVersion + '\n' +
    'Browser: ' + jscd.browser + ' ' + jscd.browserMajorVersion +
    ' (' + jscd.browserVersion + ')\n' +
    'Mobile: ' + jscd.mobile + '\n' +
    'Flash: ' + jscd.flashVersion + '\n' +
    'Cookies: ' + jscd.cookies + '\n' +
    'Screen Size: ' + jscd.screen + '\n\n' +
    'Full User Agent: ' + navigator.userAgent + '\n\n' +
    'IP: ' + ipInfo.ip + '\n' +
    'City: ' + ipInfo.city + '\n' +
    'Region: ' + ipInfo.region + '\n' +
    'Latitude: ' + ipInfo.latitude + '\n' +
    'Longtitude: ' + ipInfo.longtitude
);
/*
    Hiển thị khoản thời gian so với thời gian hiện tại
    Ví dụ sử dụng:
    Input:
    <abbr class="timeago" title="2020-04-17T09:24:17Z">2011-12-17T09:24:17Z1</abbr>
    <abbr class="timeago" title="December 17, 2015">December 17, 201o</abbr>
    <time class="timeago" datetime="2016-01-17T09:24:17Z">2013-01-17T09:24:17Z</time>
    <span class="timeago" title="1372407476">1372407476</span>
    Output:
    javascript, timeago, time, date
    about a month ago
    4 years ago
    4 years ago
    7 years ago
*/
function getTimeAgo(selector) {

    var templates = {
        prefix: "",
        suffix: " trước",
        seconds: "Hiện tại",
        minute: "1 phút",
        minutes: "%d phút",
        hour: "1 giờ",
        hours: "%d giờ",
        day: "Hôm qua",
        days: "%d ngày",
        month: "1 tháng",
        months: "%d tháng",
        year: "1 năm",
        years: "%d năm"
    };
    //prefix: "",
    //suffix: " ago",
    //seconds: "less than a minute",
    //minute: "about a minute",
    //minutes: "%d minutes",
    //hour: "about an hour",
    //hours: "about %d hours",
    //day: "a day",
    //days: "%d days",
    //month: "about a month",
    //months: "%d months",
    //year: "about a year",
    //years: "%d years"
    var template = function (t, n) {
        return templates[t] && templates[t].replace(/%d/i, Math.abs(Math.round(n)));
    };

    var timer = function (time) {
        if (!time) return;
        time = time.replace(/\.\d+/, ""); // remove milliseconds
        time = time.replace(/-/, "/").replace(/-/, "/");
        time = time.replace(/T/, " ").replace(/Z/, " UTC");
        time = time.replace(/([\+\-]\d\d)\:?(\d\d)/, " $1$2"); // -04:00 -> -0400
        time = new Date(time * 1000 || time);

        var now = new Date();
        var seconds = ((now.getTime() - time) * .001) >> 0;
        var minutes = seconds / 60;
        var hours = minutes / 60;
        var days = hours / 24;
        var years = days / 365;

        return templates.prefix + (
        seconds < 45 && template('seconds', seconds) || seconds < 90 && template('minute', 1) || minutes < 45 && template('minutes', minutes) || minutes < 90 && template('hour', 1) || hours < 24 && template('hours', hours) || hours < 42 && template('day', 1) || days < 30 && template('days', days) || days < 45 && template('month', 1) || days < 365 && template('months', days / 30) || years < 1.5 && template('year', 1) || template('years', years)); //+templates.suffix
    };

    var elements = document.getElementsByClassName('timeago');
    for (var i in elements) {
        var $this = elements[i];
        if (typeof $this === 'object') {
            $this.innerHTML = timer($this.getAttribute('title') || $this.getAttribute('datetime'));
        }
    }
    // update time every minute
    setTimeout(getTimeAgo, 60000);

};


//Get Map Device IP
function initLatiLongMap(latitude, longitude) {
    var posVietNam = { lat: 16.4498, lng: 107.5624 };
    var zoomSize = 5;
    if (latitude && longitude) {
        posVietNam = { lat: parseFloat(latitude), lng: parseFloat(longitude) };
        zoomSize = 9;
    }
    var map = new google.maps.Map(document.getElementById('map'), {
        zoom: zoomSize,
        center: posVietNam,
        mapTypeControl: false,
        zoomControl: false,
        scaleControl: false,
        streetViewControl: false,
        rotateControl: false,
        fullscreenControl: false
    });
    marker = new google.maps.Marker({
        map: map,
        draggable: false,
        animation: google.maps.Animation.DROP,
        position: posVietNam
    });
    google.maps.event.addListener(marker, 'dragend', function (event) {
        document.getElementById("Latitude").value = this.getPosition().lat();
        document.getElementById("Longitude").value = this.getPosition().lng();
    });
    //autocomplete = new google.maps.places.Autocomplete(
    //    (document.getElementById('address')), { types: ['geocode'] });
    var input = document.getElementById('Address');
    autocomplete = new google.maps.places.Autocomplete(input);
    autocomplete.addListener('place_changed', fillInAddress);
}

function fillInAddress() {
    // Get the place details from the autocomplete object.
    var place = autocomplete.getPlace();
    document.getElementById("LatiLongTude").value = place.geometry.location.lat() + "," + place.geometry.location.lng();
    var pos = { lat: place.geometry.location.lat(), lng: place.geometry.location.lng() };
    var map = new google.maps.Map(document.getElementById('map'), {
        zoom: 9,
        center: pos,
        mapTypeControl: false,
        zoomControl: false,
        scaleControl: false,
        streetViewControl: false,
        rotateControl: false,
        fullscreenControl: false
    }); var image = {
        url: "/assets/client/img/gmap_marker.png",
        anchor: new google.maps.Point(25, 25),
        scaledSize: new google.maps.Size(45, 45)
    };
    marker = new google.maps.Marker({
        map: map,
        draggable: false,
        animation: google.maps.Animation.DROP,
        position: pos,
        //icon: image
    });
    google.maps.event.addListener(marker, 'dragend', function (event) {
        document.getElementById("LatiLongTude").value = this.getPosition().lat() + "," + this.getPosition().lng();
    });
}

function getLatitudeLongitude(callback, address) {
    address = address || 'Ferrol, Galicia, Spain';
    geocoder = new google.maps.Geocoder();
    if (geocoder) {
        geocoder.geocode({
            'address': address
        }, function (results, status) {
            if (status == google.maps.GeocoderStatus.OK) {
                callback(results[0]);
            }
        });
    }
}


/*
    Ký tự icon tới image icon
*/
var iconFace = {
    smiley: ':smiley:',//:)
    smile: ':smile:',//^_^,^-^
    grinning: ':grinning:',//:D
    open_mouth: ':open_mouth:',//:o,:0,:O
    disappointed: ':disappointed:',//:(
    expressionless: ':expressionless:',//-_-
    grin: ':grin:',//:v
    heart: ':heart:',//<3 "&lt;3"
    like: ':+1:',//(y)
    confounded: ':confounded:',   //:3
    stuck_out_tongue: ':stuck_out_tongue:'//:P
}
function isValidURLandCodeIcon(string) {
    var res = string.match(/(http(s)?:\/\/.)?(www\.)?[-a-zA-Z0-9@:%._\+~#=]{2,256}\.[a-z]{2,6}\b([-a-zA-Z0-9@:%_\+.~#?&//=]*)/g);
    if (res != null) {
        string = string.replace(res, "<a href='" + res + "' target='_blank' style='text-decoration:underline'>" + res + "</a>");
    }
    else if (string.toLowerCase().includes(":)h")) {
        string = string.replace(':)H', '<img style="display:inline-block;width:25px;height:25px;border-radius:30px" src="' + _Host + 'assets/client/libs/emoji-picker/img/me.jpg" alt="" />');
    }
    else if (string.toLowerCase().includes("h_s")) {
        string = string.replace('h_s', '<img style="display:inline-block;width:25px;height:25px;border-radius:30px" src="' + _Host + 'assets/client/libs/emoji-picker/img/h_s.jpg" alt="" />');
    }
    else if (string.includes(":)")) {
        string = string.replace(':)', returnIcon(iconFace.smiley, -25, 0));
    }
    else if (string.includes("^-^") || string.includes("^_^")) {
        string = string.replace('^-^', returnIcon(iconFace.smile, 0, 0));
        string = string.replace('^_^', returnIcon(iconFace.smile, 0, 0));
    }
    else if (string.includes(":D")) {
        string = string.replace(':D', returnIcon(iconFace.grinning, -50, 0));
    }
    else if (string.includes(":o") || string.includes(":0") || string.includes(":O")) {
        string = string.replace(':o', returnIcon(iconFace.open_mouth, -550, 25));
        string = string.replace(':0', returnIcon(iconFace.open_mouth, -550, 25));
        string = string.replace(':O', returnIcon(iconFace.open_mouth, -550, 25));
    }
    else if (string.includes(":(")) {
        string = string.replace(':(', returnIcon(iconFace.disappointed, -475, 0));
    }
    else if (string.includes("-_-")) {
        string = string.replace('-_-', returnIcon(iconFace.expressionless, -75, -50));
    }
    else if (string.includes(":v")) {
        string = string.replace(':v', returnIcon(iconFace.grin, -375, 0));
    }
    else if (string.includes("&lt;3")) {
        string = string.replace('&lt;3', returnIcon(iconFace.heart, -250, -150));
    }
    else if (string.includes("(y)")) {
        string = string.replace('(y)', returnIcon(iconFace.like, -600, -75));
    }
    else if (string.includes(":3")) {
        string = string.replace(':3', returnIcon(iconFace.confounded, -225, -25));
    }
    else if (string.includes(":P")) {
        string = string.replace(':P', returnIcon(iconFace.stuck_out_tongue, -325, 0));
    }
    return string;
};
function returnIcon(code, positionX, positionY) {
    var imageIcon = '<img src="' + _Host + 'assets/client/libs/emoji-picker/img/blank.gif" class="img" style="display:inline-block;width:25px;height:25px;background:url(' + "'" + _Host + "assets/client/libs/emoji-picker/img/emoji_spritesheet_0.png'" + ') ' + positionX + 'px ' + positionY + 'px no-repeat;background-size:675px 175px;" alt="' + code + '">';
    return imageIcon
}



/*
* Get time chat current
*/
function showTimeChat() {
    date = new Date();
    var hours = date.getHours();
    var minutes = date.getMinutes();
    var ampm = hours >= 12 ? 'PM' : 'AM';
    hours = hours % 12;
    hours = hours ? hours : 12; // the hour '0' should be '12'
    minutes = minutes < 10 ? '0' + minutes : minutes;
    return hours + ':' + minutes + ' ' + ampm;
}

/*
* Audio notification new message
*/
function playAudioNotifyMessage() {
    var audio = new Audio('' + _Host + 'assets/livechat/audio/notify-message-chat.mp3');
    audio.play();
}

var Timer = function (opts) {
    var self = this;

    self.opts = opts || {};
    self.element = opts.element || null;
    self.minutes = opts.minutes || 0;
    self.seconds = opts.seconds || 30;

    self.start = function () {
        self.interval = setInterval(countDown, 1000);
    };

    self.stop = function () {
        clearInterval(self.interval);
        $('.box-reconecting').removeClass('showing');
    };

    function countDown() {
        self.seconds--;
        if (self.minutes == 0 && self.seconds == 0) {
            self.stop();
        }

        if (self.seconds < 0) {
            self.seconds = 59;
            self.minutes--;
        }

        if (self.seconds <= 9) { self.seconds = '0' + self.seconds; }

        self.element.textContent = self.seconds;// self.minutes + ' : ' + self.seconds
    }
};

