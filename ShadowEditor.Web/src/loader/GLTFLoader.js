import BaseLoader from './BaseLoader';

/**
 * GLTFLoader
 * @author tengge / https://github.com/tengge1
 */
function GLTFLoader() {
    BaseLoader.call(this);
}

GLTFLoader.prototype = Object.create(BaseLoader.prototype);
GLTFLoader.prototype.constructor = GLTFLoader;

GLTFLoader.prototype.load = function (url, options) {
    return new Promise(resolve => {
        this.require([
            'DRACOLoader',
            'GLTFLoader'
        ]).then(() => {
            var loader = new THREE.GLTFLoader();
            // THREE.DRACOLoader.setDecoderPath('assets/js/libs/draco/gltf/');
            loader.setDRACOLoader(new THREE.DRACOLoader());

            loader.load(url, result => {
                var obj3d = result.scene;

                obj3d._obj = result;
                obj3d._root = result.scene;

                if (result.animations && result.animations.length > 0) {
                    Object.assign(obj3d.userData, {
                        animNames: result.animations.map(n => n.name),
                        scripts: [{
                            id: null,
                            name: `${options.Name}${_t('Animation')}`,
                            type: 'javascript',
                            source: this.createScripts(options.Name),
                            uuid: THREE.Math.generateUUID()
                        }]
                    });
                }
                resolve(obj3d);
            }, undefined, () => {
                resolve(null);
            });
        });
    });
};

GLTFLoader.prototype.createScripts = function (name) {
    return `var mesh = this.getObjectByName('${name}');\n\n` +
        `var obj = mesh._obj;\n\n` +
        `var root = mesh._root;\n\n` +
        `var mixer = new THREE.AnimationMixer(root);\n\n` +
        `mixer.clipAction(obj.animations[0]).play();\n\n` +
        `function update(clock, deltaTime) { \n    mixer.update(deltaTime); \n}`;
};

export default GLTFLoader;