window.fastTextEditor = {
	editors: new Map(),
	
	computeHash: function (str) {
		let hash = 5381;
		for (let i = 0; i < str.length; i++) {
			hash = ((hash << 5) + hash) ^ str.charCodeAt(i); 
		}
		return hash >>> 0;
	},

	init: function (editorId, initialText, dotNetRef) {
		const element = document.getElementById(editorId);
		if (!element) return null;

		const cleanHash = this.computeHash(initialText || "");

		if (this.editors.has(editorId)) {
			const existing = this.editors.get(editorId);
			existing.dotNetRef = dotNetRef;
			existing.originalHash = cleanHash;
			
			existing.checkDirty();
			return true;
		}

		// Only set value if the textarea is actually empty (first load)
		if (!element.value && initialText) {
			element.value = initialText;
		}

		const editor = {
			element,
			dotNetRef,
			originalHash: cleanHash,
			isDirty: false,
			checkTimeout: null,

			checkDirty: function () {
				const currentHash = window.fastTextEditor.computeHash(element.value);
				const newIsDirty = currentHash !== this.originalHash;

				if (newIsDirty !== this.isDirty) {
					this.isDirty = newIsDirty;
					try { 
						this.dotNetRef.invokeMethodAsync('OnJsDirtyStateChanged', this.isDirty); 
					} catch (e) { console.error(e); }
				}
			},
			getText: function () { return element.value; },
			dispose: function () {
				element.removeEventListener('input', this.inputHandler);
				clearTimeout(this.checkTimeout);
			}
		};

		editor.inputHandler = () => {
			clearTimeout(editor.checkTimeout);
			editor.checkTimeout = setTimeout(() => { editor.checkDirty(); }, 100);
		};

		element.addEventListener('input', editor.inputHandler);
		this.editors.set(editorId, editor);
		editor.checkDirty();
		return true;
	},

	getText: function (editorId) {
		const editor = this.editors.get(editorId);
		return editor ? editor.getText() : "";
	},

	dispose: function (editorId) {
		const editor = this.editors.get(editorId);
		if (editor) {
			editor.dispose();
			// We DON'T delete from the map immediately if we want to preserve 
			// some state, but usually, the DOM element is gone anyway.
			this.editors.delete(editorId);
		}
	}
};