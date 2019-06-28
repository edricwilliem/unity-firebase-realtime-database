var FirebaseWebGLESBridge = {
    $es:{
        eventSourceInstances:{},
        nextInstanceId:1,
        Set : function(event){
            es.eventSourceInstances[es.nextInstanceId] = event;
            return es.nextInstanceId++;
        },
        Get : function(id){
            return es.eventSourceInstances[id];
        },
        Remove:function(id){
            delete es.eventSourceInstances[id];
        },
        _callOnError: function(errCallback,id,reason){
            if(reason){
                var bufferSize = lengthBytesUTF8(reason) + 1;
                var buffer = _malloc(bufferSize);
                stringToUTF8(reason, buffer,bufferSize);
                Runtime.dynCall('vii', errCallback, [id, buffer]);
				_free(buffer);
			}
			else
				Runtime.dynCall('vii', errCallback, [id, null]);
        }
    },

    CreateEventSource: function(urlPtr, withCredentials, onOpen, onMessage, onError)
	{
		var url = encodeURI(Pointer_stringify(urlPtr)).replace(/\+/g, '%2B');

		var event = {
			onError: onError
		};

		var id = es.nextInstanceId;

		console.log(id + ' ES_Create(' + url + ', ' + withCredentials + ')');

		event.eventImpl = new EventSource(url, { withCredentials: withCredentials != 0 ? true : false } );

		event.eventImpl.onopen = function() {
			console.log(id + ' ES_Create - onOpen');

			Runtime.dynCall('vi', onOpen, [id]);
		};

		function AllocString(str) {
			if (str != undefined)
			{
				var bufferSize = lengthBytesUTF8(str) + 1;
				var buff = _malloc(bufferSize);
				stringToUTF8(str, buff,bufferSize);
				return buff;
			}

			return 0;
		}

		event.eventImpl.addEventListener('put',function (e){
			console.log("put");
			var eventBuffer = AllocString("put");
			var dataBuffer = AllocString(e.data);
			Runtime.dynCall('viii', onMessage, [id, eventBuffer, dataBuffer]);

			if (eventBuffer != 0)
				_free(eventBuffer);

			if (dataBuffer != 0)
				_free(dataBuffer);
		});

		event.eventImpl.addEventListener('patch',function (e){
			console.log("patch");
			var eventBuffer = AllocString("patch");
			var dataBuffer = AllocString(e.data);
			Runtime.dynCall('viii', onMessage, [id, eventBuffer, dataBuffer]);

			if (eventBuffer != 0)
				_free(eventBuffer);

			if (dataBuffer != 0)
				_free(dataBuffer);
		});

		event.eventImpl.addEventListener('keep-alive',function (e){
			console.log("keep_alive");
			var eventBuffer = AllocString("keep-alive");
			Runtime.dynCall('viii', onMessage, [id, eventBuffer, null]);

			if (eventBuffer != 0)
				_free(eventBuffer);
		});

		event.eventImpl.addEventListener('auth_revoked',function (e){
			console.log("auth_revoked");
			var eventBuffer = AllocString("auth_revoked");
			Runtime.dynCall('viii', onMessage, [id, eventBuffer, null]);

			if (eventBuffer != 0)
				_free(eventBuffer);
		});

		event.eventImpl.onerror = function(e) {
			console.log(id + 'ES_Create - onError');
			es._callOnError(onError, id, "Unknown Error!");
		};

		return es.Set(event);
	},

	CloseEventSource: function(id)
	{
		console.log(id + ' ES_Close');
		var event = es.Get(id);
		if(event){
			event.eventImpl.close();
			es.Remove(id);
		}
	}
};

autoAddDeps(FirebaseWebGLESBridge, '$es');
mergeInto(LibraryManager.library, FirebaseWebGLESBridge);