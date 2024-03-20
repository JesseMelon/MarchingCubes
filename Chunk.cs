using Godot;
using System;
using System.Collections.Generic;

/*Godot mesh gen gameplan.
declare a surface tool, and lists for mesh info. verts, uvs, normals, and indices as needed.
declare a surface array to pull all the data arrays into one object. this must be the godot.collections.array type
on ready, addChild meshInstance3d node to hold our finished mesh
resize the godot.collections.array suraceArray to (int)Mesh.ArrayType.Max. This just sets the length to that of an arraymesh
add to lists as needed, they are variable length
when done editing lists, convert to arrays and feed them to surfaceArray
feed surfaceArray to surfaceTool to get an arrayMesh
use the arraymesh to set the mesh property of the meshInstance3d.
you should now have a rendered procedural mesh
*/

//TODO add to node group called "terrain" or another called "destructable"

namespace Project;

//Generate voxel terrain with noise
public partial class Chunk : StaticBody3D
{
    //terrainInstance
    MeshInstance3D meshInstance3D = new();
    //terrain collider
    CollisionShape3D collisionShape = new();
    //surfacetool(for generating normals)
    SurfaceTool surfaceTool = new();


    //mesh gen data members
    Godot.Collections.Array surfaceArray = new(); //surface array is fed to surface tool after being loaded with individual arrays. Must be godot collection type
    readonly List<Vector3> vertices = new();//these are the arrays modified with mesh data and passed to surfaceArray for rendering.
    readonly List<int> indices = new();
    readonly List<Color> colour = new();

    BaseMaterial3D material;
    
    bool useVertexColors = false;
    bool isSmooth = true;
    int width;
    int height;

    const float terrainSurface = 0.5f; 
    float[,,] terrainMap;

    //constructor
    public Chunk(Vector3I _position, int _width, int _height)
    {
        CreateChunk(_position, _width, _height);    
    }
    public Chunk() { }

    private void CreateChunk(Vector3I _position, int _width, int _height)
    {
        //init
        width = _width;
        height = _height;
        this.Position = _position;
        AddToGroup("Terrain", true);
        AddChild(meshInstance3D);//apply the mesh instance and collider as children (otherwise theyd remain theoretical)
        AddChild(collisionShape);

        surfaceArray.Resize((int)Mesh.ArrayType.Max); //surface array is of the godot array type, this declares the length to 13 for use with surface tool
        terrainMap = new float[width + 1, height + 1, width + 1]; //here incase width and height need to be variables over constant
                                                                  // fastNoise.NoiseType = noiseType;//set type of noise
        if (useVertexColors && material != null)
        {
            //material.CreatePlaceholder();
            material.VertexColorUseAsAlbedo = useVertexColors;
        }

        ////////////////////////////////////////////////////////////////////////////////////
        //do

        PopulateTerrainMap();
        CreateMeshData();
        BuildMesh();
        MeshDataTool meshDataTool = new();
        meshDataTool.CreateFromSurface(meshInstance3D.Mesh as ArrayMesh, 0);
        GD.Print(meshDataTool.GetVertexCount());
    }

    //Sample noise and add it to the terrainMap
    void PopulateTerrainMap()
    {
        for (int x = 0; x < width + 1; x++)
        {
            for (int y = 0; y < height + 1; y++)
            {
                for (int z = 0; z < width + 1; z++)
                {

                    float currentHeight = MarchingCubeData.GetTerrainHeight(x + (int)Position.X, y + (int)Position.Y, z + (int)Position.Z);

                    terrainMap[x, y, z] = currentHeight - y;

                }
            }
        }
    }

    //Just call marchcube
    void CreateMeshData()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < width; z++)
                {
                    MarchCube(new Vector3I(x, y, z));
                }
            }
        }
    }

    //uses bitshift to index the appropriate configuration in the Triangles LUT based on the cube's proximity to the noise values (whish is gathered in MarchCube)
    static int GetCubeConfiguration(float[] cube)
    {
        int configurationIndex = 0;

        //basically, sets the config index number based on the bytewise value of the terrain surface. This is how we reference the right item from the LUT.
        //so, for each of the 8 bits, it indexes to the appropriate subcategory by inserting the bit. No arithmatic necessary. works because we have 2^8 values in the LUT.
        for (int i = 0; i < 8; i++)
        {
            if (cube[i] > terrainSurface)
            {
                configurationIndex |= 1 << i;
            }
        }

        return configurationIndex;
    }

    //makes a cube for position. Cube detects collisions with noise values to configure the shape. Generates geometry data if necessary
    void MarchCube(Vector3I position)
    {
        //create an array of floats for each corner of a cube and get a value for each point based on terrain map
        float[] cube = new float[8];
        for (int i = 0; i < 8; i++)
        {
            cube[i] = SampleTerrain(position + MarchingCubeData.CornerTable[i]);
        }

        int configIndex = GetCubeConfiguration(cube);

        if (configIndex == 0 || configIndex == 255) return;

        int edgeIndex = 0;

        for (int i = 0; i < 5; i++)//for each triangle
        {
            for (int j = 0; j < 3; j++)//for each point in triangle
            {
                int index = MarchingCubeData.TriangleTable[configIndex, edgeIndex];

                if (index == -1) return; //done tri

                Vector3 vert1 = position + MarchingCubeData.CornerTable[MarchingCubeData.EdgeIndices[index, 0]]; //check for terrain between these points
                Vector3 vert2 = position + MarchingCubeData.CornerTable[MarchingCubeData.EdgeIndices[index, 1]];
                Vector3 vertPosition;
                if (isSmooth)
                {
                    float vert1Sample = cube[MarchingCubeData.EdgeIndices[index, 0]];
                    float vert2Sample = cube[MarchingCubeData.EdgeIndices[index, 1]];

                    //calculate the difference between verts on terrain
                    float diff = vert2Sample - vert1Sample;
                    if (diff == 0) //this means the terrain does not intersect
                        diff = terrainSurface;
                    else
                        diff = (terrainSurface - vert1Sample) / diff;
                    //if intersecting, get precise point along edge
                    vertPosition = vert1 + ((vert2 - vert1) * diff);
                }
                else
                {
                    vertPosition = (vert1 + vert2) / 2f; //this creates position for the new vert within the detection cube
                }

                vertices.Add(vertPosition); //add to lists
                indices.Add(vertices.Count - 1);//add index of the tri.

                if (useVertexColors && material != null)
                {
                    if (position.Y > 1.5) colour.Equals(Colors.DarkOliveGreen);
                    else colour.Equals(Colors.DarkKhaki);
                }
                edgeIndex++; // to measure next edge
            }
        }
    }

    public void PlaceTerrain(Vector3 position)
    {
        Vector3I v3Int = new(Mathf.CeilToInt(position.X), Mathf.CeilToInt(position.Y), Mathf.CeilToInt(position.Z));
        terrainMap[v3Int.X, v3Int.Y, v3Int.Z] = 1f;
        CreateMeshData();
        BuildMesh();
    }

    //we already populated the terrainMap, this samples the map at a given point
    float SampleTerrain(Vector3I point)
    {
        return terrainMap[point.X, point.Y, point.Z];
    }

    //self explanatory
    void ClearMeshData()
    {
        vertices.Clear(); indices.Clear();//flush temp arrays
    }

    //finalizes mesh data
    void BuildMesh()
    {
        //gather data streams into surface array
        surfaceArray[(int)Mesh.ArrayType.Vertex] = vertices.ToArray();  //finalize temp arrays by passing them into surface array
        surfaceArray[(int)Mesh.ArrayType.Index] = indices.ToArray();
        if (useVertexColors && material != null) surfaceArray[(int)Mesh.ArrayType.Color] = colour.ToArray();

        ArrayMesh arrayMesh = new(); //arraymesh will be the product of the surface array data


        arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);  //feed surface array to arrayMesh (sometimes surface array is labelled "data")
        arrayMesh.SurfaceSetMaterial(0, material);
        surfaceTool.CreateFrom(arrayMesh, 0); //feed arraymesh to surfaceTool (strictly for generating normals)
        surfaceTool.GenerateNormals();
        arrayMesh = surfaceTool.Commit(); //now overwrite the array but with normals this time. 
        collisionShape.Shape = arrayMesh.CreateTrimeshShape();
        meshInstance3D.Mesh = arrayMesh; //update the instance
    }
}
