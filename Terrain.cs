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

namespace Project
{
    [Tool]

    //Generate voxel terrain with noise
    public partial class Terrain : StaticBody3D
    {
        //configurations
        [Export] bool smoothTerrain = true;

        [ExportGroup("Material")]
        [Export] BaseMaterial3D material;
        [Export] bool useVertexColors = false;

        [ExportGroup("Size")]
        [Export] int width = 32;//# blocks wide (x and z), positive quadrant
        [Export] int height = 8;//# blocks above origin

        [ExportGroup("Noise")]
        [Export] FastNoiseLite.NoiseTypeEnum noiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
        [Export] FastNoiseLite.FractalTypeEnum fractalType = FastNoiseLite.FractalTypeEnum.None;
        [Export] float fractalgain = 0.5f;
        //[Export]
        //float 

        //declarations
        FastNoiseLite fastNoise = new();
        MeshInstance3D meshInstance3D = new();//mesh instance for rendering, becomes instance of the arraymesh data
        CollisionShape3D collisionShape = new();
        Godot.Collections.Array surfaceArray = new(); //surface array is fed to surface tool after being loaded with individual arrays. Must be godot collection type
        SurfaceTool surfaceTool = new(); //for normals

        readonly List<Vector3> vertices = new();//these are the arrays modified with mesh data and passed to surfaceArray for rendering.
        readonly List<int> indices = new();
        readonly List<Color> colour = new();

        const float terrainSurface = 0.5f; //middlepoint of noise detection (determines acceptance) 
        float[,,] terrainMap; //is filled with values that are the DIFFERENCE to the noise texture for each CORNER of a position (hence values + 1)

        //init stuff, one offs for terrain generation. Basically main.

        public override void _Ready()
        {
            //init
            AddChild(meshInstance3D);//apply the mesh instance to *this* object
            AddChild(collisionShape);
            surfaceArray.Resize((int)Mesh.ArrayType.Max); //surface array is of the godot array type, this declares the length to 13 for use in surface tool
            terrainMap = new float[width + 1, height + 1, width + 1]; //here incase width and height need to be variables over constant
            fastNoise.NoiseType = noiseType;//set type of noise
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

                        // get a noise value
                        float currentHeight = (float)height * Math.Clamp(fastNoise.GetNoise3D((float)x, (float)y, (float)z), 0, 2) + 1;

                        // based on the noise, update the cube data with the distance to the actual noise value. Only necessary for dual contouring (smoothness)
                        terrainMap[x, y, z] = currentHeight - y;//if the ground is upside down, reverse these operands
                    }
                }
            }
        }

        //Just call marchcube wherever relevant
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
                    if (smoothTerrain)
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
}
