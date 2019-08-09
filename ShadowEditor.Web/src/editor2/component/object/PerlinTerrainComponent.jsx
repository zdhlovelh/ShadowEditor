import { PropertyGrid, PropertyGroup, TextProperty, DisplayProperty, CheckBoxProperty, NumberProperty, IntegerProperty, SelectProperty, ButtonsProperty, Button } from '../../../third_party';
import PerlinTerrain from '../../../object/terrain/PerlinTerrain';

/**
 * 柏林地形组件
 * @author tengge / https://github.com/tengge1
 */
class PerlinTerrainComponent extends React.Component {
    constructor(props) {
        super(props);

        this.selected = null;

        this.state = {
            show: false,
            expanded: true,
            width: 1000,
            depth: 1000,
            widthSegments: 256,
            depthSegments: 256,
            quality: 80,
        };

        this.handleExpand = this.handleExpand.bind(this);
        this.handleUpdate = this.handleUpdate.bind(this);
        this.handleChange = this.handleChange.bind(this);
    }

    render() {
        const { show, expanded, width, depth, widthSegments, depthSegments, quality } = this.state;

        if (!show) {
            return null;
        }

        return <PropertyGroup title={L_PERLIN_TERRAIN} show={show} expanded={expanded} onExpand={this.handleExpand}>
            <NumberProperty label={L_WIDTH} name={'width'} value={width} onChange={this.handleChange}></NumberProperty>
            <NumberProperty label={L_DEPTH} name={'depth'} value={depth} onChange={this.handleChange}></NumberProperty>
            <IntegerProperty label={L_WIDTH_SEGMENTS} name={'widthSegments'} value={widthSegments} onChange={this.handleChange}></IntegerProperty>
            <IntegerProperty label={L_DEPTH_SEGMENTS} name={'depthSegments'} value={depthSegments} onChange={this.handleChange}></IntegerProperty>
            <NumberProperty label={L_QUALITY} name={'quality'} value={quality} onChange={this.handleChange}></NumberProperty>
        </PropertyGroup>;
    }

    componentDidMount() {
        app.on(`objectSelected.PerlinTerrainComponent`, this.handleUpdate.bind(this));
        app.on(`objectChanged.PerlinTerrainComponent`, this.handleUpdate.bind(this));
    }

    handleExpand(expanded) {
        this.setState({
            expanded,
        });
    }

    handleUpdate() {
        const editor = app.editor;

        if (!editor.selected || !(editor.selected instanceof PerlinTerrain)) {
            this.setState({
                show: false,
            });
            return;
        }

        this.selected = editor.selected;

        this.setState({
            show: true,
            width: this.selected.userData.width,
            depth: this.selected.userData.depth,
            widthSegments: this.selected.userData.widthSegments,
            depthSegments: this.selected.userData.depthSegments,
            quality: this.selected.userData.quality,
        });
    }

    handleChange(value, name) {
        if (value === null) {
            this.setState({
                [name]: value,
            });
            return;
        }

        const { width, depth, widthSegments, depthSegments, quality } = Object.assign({}, this.state, {
            [name]: value,
        });

        let terrain = new PerlinTerrain(
            width, depth, widthSegments, depthSegments, quality
        );

        const editor = app.editor;

        const index = editor.scene.children.indexOf(this.selected);

        if (index > -1) {
            editor.select(null);
            editor.scene.children[index] = terrain;
            terrain.parent = this.selected.parent;
            this.selected.parent = null;
            app.call(`objectRemoved`, this, this.selected);
            app.call(`objectAdded`, this, terrain);
            editor.select(terrain);
        }
    }
}

export default PerlinTerrainComponent;