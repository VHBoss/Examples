using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;
using System.Globalization;

public class Buildings_v11 : MonoBehaviour {

    struct NodeElement {
        public long id;
        public Vector3 position;
    }

    struct WayElement {
        public List<NodeElement> nodes;
        public Tags tags;
    }

    struct RelationElement {
        public List<Members> members;
        public Tags tags;
    }

    struct Members {
        public string type;
        public long id;
        public string role;
    }

    struct Tags {
        public bool building;
        public string part;
        public int min_level;
        public int levels;
        public float height;
        public float min_height;
        public float roof_direction;
        public float roof_height;
        public string location;
        public string roof_shape;
    }

    public float sceneScale = 0.1f;
    public Shaper shaper;
    public LineRenderer linePrefab;
    public MeshFilter meshPrefab;
    public float globalScale = 0.1f;
    public float levelSize = 3.5f;
    public bool combineMeshes;
    public float bakeCellSize = 200f;
    public int maxCombineCount = 1000;
    public Transform combineMeshRoot;
    public bool isTest;
    public TextAsset json;

    private BBox bbox;
    private Vector3d delta;
    private Dictionary<long, Vector3d> Nodes = new Dictionary<long, Vector3d>();
    private Dictionary<long, WayElement> Ways = new Dictionary<long, WayElement>();
    private Dictionary<long, RelationElement> Relations = new Dictionary<long, RelationElement>();
    private Dictionary<long, RelationElement> RelationsParts = new Dictionary<long, RelationElement>();
    private HashSet<long> Parts = new HashSet<long>();

    void Start() {
        bbox = new BBox();
        globalScale *= sceneScale;
        levelSize *= sceneScale;
        if (isTest) {
            CreateShape(json.text);
        }
    }

    public void CreateShape(string json) {
        FillData(json);
        ProccessData();
        if (combineMeshes) CombineMeshes();
    }

    void FillData(string json) {
        JSONNode N = JSON.Parse(json);
        if (isTest) {
            double lat = double.Parse(N["elements"][0]["lat"], CultureInfo.InvariantCulture);
            double lon = double.Parse(N["elements"][0]["lon"], CultureInfo.InvariantCulture);
            delta = LatLon2XY(lat, lon);
        }

        int length = N["elements"].Count;
        for (int i = 0; i < length; i++) {
            JSONNode node = N["elements"][i];
            if (node["type"].Equals("node")) {
                AddNode(node);
            } else if (node["type"].Equals("way")) {
                AddWay(node);
            } else if (node["type"].Equals("relation")) {
                AddRelation(node);
            }
        }
    }

    void AddNode(JSONNode node) {
        double lat = double.Parse(node["lat"], CultureInfo.InvariantCulture);
        double lon = double.Parse(node["lon"], CultureInfo.InvariantCulture);
        Vector3d pos = LatLon2XY(lat, lon) - delta;
        Nodes.Add(node["id"].AsLong, pos);
        bbox.AddPoint((float)pos.x, (float)pos.z);
    }

    void AddWay(JSONNode way) {
        WayElement wayElement = new WayElement();
        wayElement.nodes = new List<NodeElement>();

        int length = way["nodes"].Count;
        for (int i = 0; i < length; i++) {
            long nodeID = way["nodes"][i].AsLong;
            if (!Nodes.ContainsKey(nodeID)) {
                print("Node not found: " + nodeID);
                continue;
            }
            NodeElement node;
            node.id = nodeID;
            node.position = (Vector3)Nodes[nodeID];
            wayElement.nodes.Add(node);
        }

        wayElement.tags = GetTags(way["tags"]);
       
        long id = way["id"].AsLong;
        Ways.Add(id, wayElement);
    }

    void AddRelation(JSONNode relation) {
        RelationElement relationElement = new RelationElement();
        relationElement.members = new List<Members>();

        int length = relation["members"].Count;
        for (int i = 0; i < length; i++) {
            Members member = new Members();
            member.type = relation["members"][i]["type"];
            member.id = relation["members"][i]["ref"].AsLong;
            member.role = relation["members"][i]["role"];
            relationElement.members.Add(member);
        }

        relationElement.tags = GetTags(relation["tags"]);

        long id = relation["id"].AsLong;
        if(!string.IsNullOrEmpty(relationElement.tags.part) && relationElement.tags.part.Equals("yes"))
            RelationsParts.Add(id, relationElement);
        else
            Relations.Add(id, relationElement);
    }

    Tags GetTags(JSONNode TAGS) {
        Tags tags = new Tags();
        if (!string.IsNullOrEmpty(TAGS["building"])) tags.building = TAGS["building"].AsBool;
        if (!string.IsNullOrEmpty(TAGS["building:part"])) tags.part = TAGS["building:part"];
        if (!string.IsNullOrEmpty(TAGS["building:min_level"])) tags.min_level = TAGS["building:min_level"].AsInt;
        if (!string.IsNullOrEmpty(TAGS["building:levels"])) tags.levels = TAGS["building:levels"].AsInt;
        if (!string.IsNullOrEmpty(TAGS["height"])) {
            try {
                tags.height = float.Parse(TAGS["height"], CultureInfo.InvariantCulture);
            } catch {
                string h = TAGS["height"];
                // Бывает к числу добавляют " m"
                print(h);
                string[] s = h.Split(' ');
                tags.height = float.Parse(s[0], CultureInfo.InvariantCulture);
            }
        }
        if (!string.IsNullOrEmpty(TAGS["min_height"])) tags.min_height = float.Parse(TAGS["min_height"], CultureInfo.InvariantCulture);
        if (!string.IsNullOrEmpty(TAGS["roof:direction"])) tags.roof_direction = float.Parse(TAGS["roof:direction"], CultureInfo.InvariantCulture);
        if (!string.IsNullOrEmpty(TAGS["roof:height"])) tags.roof_height = float.Parse(TAGS["roof:height"], CultureInfo.InvariantCulture);
        if (!string.IsNullOrEmpty(TAGS["roof:shape"])) tags.roof_shape = TAGS["roof:shape"];
        if (!string.IsNullOrEmpty(TAGS["location"])) tags.location = TAGS["location"];
        return tags;
    }

    void ProccessData() {
        foreach (var way in Ways) {
            if (way.Value.tags.building)
                DrawWayBuilding(way.Value, way.Key);
            else if (!string.IsNullOrEmpty(way.Value.tags.part) && way.Value.tags.part.Equals("yes")) {
                DrawWayPart(way.Value, way.Key);
            }
        }

        foreach (var rel in RelationsParts)
            DrawRelation(rel.Value, rel.Key, false);

        foreach (var rel in Relations)
            DrawRelation(rel.Value, rel.Key, true);
    }

    //*************************** WAY *****************************

    void DrawWayBuilding(WayElement way, long id) {
        int length = way.nodes.Count;
        if (length < 3) return;

        float minHeight = way.tags.min_height;
        float height = way.tags.height;
        int minLevel = way.tags.min_level;
        int totalLevels = way.tags.levels;

        if (height == 0) {
            if (totalLevels == 0) totalLevels = 2;
            minHeight = minLevel * levelSize;
            height = totalLevels * levelSize;
        }

        Vector3[] points = new Vector3[length];
        for (int i = 0; i < length; i++) 
            points[i] = way.nodes[i].position;

        shaper.DrawBuilding(points, minHeight * globalScale, height * globalScale, minLevel, totalLevels, id.ToString());
    }

    void DrawWayPart(WayElement way, long id) {
        int length = way.nodes.Count;
        if (length < 3) return;

        Parts.Add(id);

        float minHeight = way.tags.min_height;
        float height = way.tags.height;
        int minLevel = way.tags.min_level;
        int totalLevels = way.tags.levels;

        if (height == 0) {
            if (totalLevels == 0) totalLevels = 2;
            minHeight = minLevel * levelSize;
            height = totalLevels * levelSize;
        }

        Vector3[] points = new Vector3[length];
        for (int i = 0; i < length; i++)
            points[i] = way.nodes[i].position;

        if (!string.IsNullOrEmpty(way.tags.roof_shape)) {
            DrawRoof(way.tags, points, id);
        } else {
            shaper.DrawBuilding(points, minHeight * globalScale, height * globalScale, minLevel, totalLevels, id.ToString());
        }
    }

    //*************************** END WAY *****************************

    //************************** RELATION *****************************

    void DrawRelation(RelationElement relation, long id, bool isBuilding) {

        if (isBuilding && HasParts(relation))
            return;

        List <WayElement> outer = new List<WayElement>();
        List<WayElement> inner = new List<WayElement>();

        for (int i = 0; i < relation.members.Count; i++) {
            long refID = relation.members[i].id;
            if(!isBuilding) Parts.Add(refID);

            if (relation.members[i].type.Equals("way")) {
                if (!Ways.ContainsKey(refID)) {
                    print("WAY NOT FOUND: " + refID);
                    continue;
                }
                WayElement way = Ways[refID];
                if (relation.members[i].role.Equals("inner"))
                    inner.Add(way);
                else
                    outer.Add(way);
            } else {
                print("DrawRelationBuilding: " + relation.members[i].type);
            }
        }

        if (outer.Count == 0) return;

        if (!string.IsNullOrEmpty(relation.tags.location))
            if (relation.tags.location.Equals("underground")) return;

        List<WayElement> outerWeld = WeldShape(outer);
        List<WayElement> innerWeld = WeldShape(inner);
        
        List<Vector3> outerShape = new List<Vector3>();
        for (int i = 0; i < outerWeld[0].nodes.Count; i++)
            outerShape.Add(outerWeld[0].nodes[i].position);

        //Optimize(ref outerShape);

        List<List<Vector3>> innerShape = new List<List<Vector3>>();
        for (int i = 0; i < innerWeld.Count; i++) {
            List<Vector3> hole = new List<Vector3>();
            for (int j = 0; j < innerWeld[i].nodes.Count; j++)
                hole.Add(innerWeld[i].nodes[j].position);

            Optimize(ref hole);
            if (hole.Count > 2) innerShape.Add(hole);
        }

        float minHeight = relation.tags.min_height;
        float height = relation.tags.height;
        int minLevel = relation.tags.min_level;
        int totalLevels = relation.tags.levels;

        if (height == 0) {
            if (totalLevels == 0) totalLevels = 2;
            minHeight = minLevel * levelSize;
            height = totalLevels * levelSize;
        }

        if (!string.IsNullOrEmpty(relation.tags.roof_shape)) {
            DrawRoof(relation.tags, outerShape.ToArray(), id);
        } else {
            try {
                shaper.DrawCap(outerShape, innerShape, height * globalScale, id.ToString());//Cap
            } catch {
                print(id);
            }

            shaper.DrawBuilding(outerShape.ToArray(), minHeight * globalScale, height * globalScale, minLevel, totalLevels, "Outer " + id, false);
            for (int i = 0; i < innerShape.Count; i++) {
                shaper.DrawBuilding(innerShape[i].ToArray(), minHeight * globalScale, height * globalScale, minLevel, totalLevels, "Inner", false);
            }
        }
    }

    void DrawRoof(Tags tags, Vector3[] points, long id) {
        float minHeight = tags.min_height;
        float height = tags.height;
        int minLevel = tags.min_level;
        int totalLevels = tags.levels;

        if (height == 0) {
            if (totalLevels == 0) totalLevels = 2;
            minHeight = minLevel * levelSize;
            height = totalLevels * levelSize;
        }

        float roofHeight = minHeight - height;
        if (tags.roof_height > 0)
            roofHeight = tags.roof_height;

        minHeight *= globalScale;
        height *= globalScale;
        roofHeight *= globalScale;

        if (tags.roof_shape.Equals("pyramidal")) shaper.DrawPyramidal(points, minHeight, height, roofHeight, "pyramidal" + id.ToString());
        else if (tags.roof_shape.Equals("gabled")) shaper.DrawGablet(points, minHeight, height, roofHeight, "gabled" + id.ToString());
        else if (tags.roof_shape.Equals("round")) shaper.DrawGablet(points, minHeight, height, roofHeight, "round" + id.ToString());
        else if (tags.roof_shape.Equals("flat")) shaper.DrawFlat(points, height, "flat" + id.ToString());
        //else print("ROOF: " + tags.roof_shape);
    }

    bool HasParts(RelationElement relation) {
        for (int i = 0; i < relation.members.Count; i++) {
            long memberID = relation.members[i].id;
            if (Parts.Contains(memberID))
                return true;
        }
        return false;
    }

    List<WayElement> WeldShape(List<WayElement> wayElement) {
        //Create Duplicate of array
        List<WayElement> shape = new List<WayElement>();
        for (int i = 0; i < wayElement.Count; i++) {
            WayElement way = new WayElement();
            way.nodes = new List<NodeElement>(wayElement[i].nodes);
            shape.Add(way);
        }

        for (int i = 0; i < shape.Count; i++) {
            int pointsCount = shape[i].nodes.Count;
            long start = shape[i].nodes[0].id;
            long end = shape[i].nodes[pointsCount - 1].id;
            if (start == end) continue;

            for (int j = 1; j < shape.Count; j++) {
                if (shape[i].nodes == shape[j].nodes) continue;
                int pointsCount2 = shape[j].nodes.Count;
                long start2 = shape[j].nodes[0].id;
                long end2 = shape[j].nodes[pointsCount2 - 1].id;
                //TODO удалять дубликаты
                if (end == start2) {
                    shape[i].nodes.AddRange(shape[j].nodes);
                    shape.Remove(shape[j]);
                    i = -1;
                    break;
                } else
                if (start == end2) {
                    shape[j].nodes.AddRange(shape[i].nodes);
                    shape.Remove(shape[i]);
                    i = -1;
                    break;
                } else if (end == end2) {
                    List<NodeElement> tmp = new List<NodeElement>(shape[j].nodes);
                    tmp.Reverse();
                    shape[i].nodes.AddRange(tmp);
                    shape.Remove(shape[j]);
                    i = -1;
                    break;
                }
            }
        }
        return shape;
    }

    void Optimize(ref List<Vector3> shape) {
        for (int i = 0; i < shape.Count - 1; i++) {
            if (Vector3.SqrMagnitude(shape[i] - shape[i + 1]) < 0.001f) {
                shape.Remove(shape[i + 1]);
                i--;
            }
        }
    }

    void CombineMeshes() {
        float width = bbox.Width();
        float height = bbox.Height();
        int w = Mathf.RoundToInt(width / bakeCellSize);
        int h = Mathf.RoundToInt(height / bakeCellSize);
        bakeCellSize = width / w;
        float bakeCellSizeY = height / h;
        Vector3 size = new Vector3(0.5f * bakeCellSize, 20, 0.5f * bakeCellSizeY);
        Vector3 StartPos = new Vector3(bbox.Left(), 0, bbox.Bottom()) + new Vector3(0.5f * bakeCellSize, 0, -0.5f * bakeCellSizeY);

        for (int i = 0; i < w; i++) {
            for (int j = 0; j < h; j++) {
                Vector3 pos = new Vector3(i * bakeCellSize, 10, -j * bakeCellSizeY) + StartPos;

                //GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                //go.transform.localScale = size*2f;
                //go.transform.position = pos;

                RaycastHit[] hits = Physics.BoxCastAll(pos, size, Vector3.down);
                int length = hits.Length;
                MeshFilter[] meshFilters = new MeshFilter[length];
                for (int k = 0; k < length; k++) {
                    Collider col = hits[k].collider;
                    meshFilters[k] = col.GetComponent<MeshFilter>();
                    col.enabled = false;
                }
                Combine(meshFilters);
            }
        }
    }

    void Combine(MeshFilter[] meshFilters) {
        int iterations = meshFilters.Length / maxCombineCount;
        CombineInstance[] combine;
        MeshFilter mf;
        int idx = 0;
        for (int i = 0; i < iterations; i++) {
            combine = new CombineInstance[maxCombineCount];
            for (int j = 0; j < maxCombineCount; j++) {
                combine[j].mesh = meshFilters[idx].sharedMesh;
                combine[j].transform = meshFilters[idx].transform.localToWorldMatrix;
                meshFilters[idx].gameObject.SetActive(false);
                idx++;
            }
            mf = Instantiate(meshPrefab, transform);
            mf.mesh = new Mesh();
            mf.mesh.CombineMeshes(combine);
            mf.name = "Buildings" + i.ToString("00");
        }

        int rest = meshFilters.Length % maxCombineCount;
        combine = new CombineInstance[rest];

        for (int j = 0; j < rest; j++) {
            combine[j].mesh = meshFilters[idx].sharedMesh;
            combine[j].transform = meshFilters[idx].transform.localToWorldMatrix;
            meshFilters[idx].gameObject.SetActive(false);
            idx++;
        }
        mf = Instantiate(meshPrefab, combineMeshRoot);
        mf.mesh = new Mesh();
        mf.mesh.CombineMeshes(combine);
        mf.name = "Buildings" + (iterations).ToString("00");
    }

    Vector3d LatLon2XY(double lat, double lon) {
        Vector3d data = new Vector3d();
        lat = Mathd.Min(89.5f, Mathd.Max(lat, -89.5f));
        data.x = 6378137f * lon * Mathd.Deg2Rad;
        data.z = 6378137f * Mathd.Log(Mathd.Tan(Mathd.PI / 4f + lat * Mathd.Deg2Rad / 2f));
        data *= sceneScale;
        return data;
    }

    // ############## DEBUG ###############

    void CreateLine(List<NodeElement> nodes, float height, string lineName) {
        LineRenderer line = Instantiate(linePrefab);
        line.name = lineName;
        line.positionCount = nodes.Count;
        List<Vector3> points = new List<Vector3>();
        for (int i = 0; i < nodes.Count; i++) {
            points.Add(new Vector3(nodes[i].position.x, nodes[i].position.z, -height));
        }
        line.SetPositions(points.ToArray());
    }
}
