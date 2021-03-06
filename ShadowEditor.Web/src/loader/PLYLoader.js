import BaseLoader from './BaseLoader';

/**
 * PLYLoader
 * @author tengge / https://github.com/tengge1
 */
function PLYLoader() {
    BaseLoader.call(this);
}

PLYLoader.prototype = Object.create(BaseLoader.prototype);
PLYLoader.prototype.constructor = PLYLoader;

PLYLoader.prototype.load = function (url) {
    return new Promise(resolve => {
        this.require('PLYLoader').then(() => {
            var loader = new THREE.PLYLoader();

            loader.load(url, geometry => {
                geometry.computeVertexNormals();
                var material = new THREE.MeshStandardMaterial();
                var mesh = new THREE.Mesh(geometry, material);
                resolve(mesh);
            }, undefined, () => {
                resolve(null);
            });
        });
    });
};

export default PLYLoader;