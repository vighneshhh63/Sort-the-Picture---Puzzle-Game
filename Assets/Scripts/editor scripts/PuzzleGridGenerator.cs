using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

// This is a SPECIAL script that adds a button inside Unity itself
// (not something that runs in the game - it's a helper TOOL for us, the developer)
public class PuzzleGridGenerator : EditorWindow
{
    public Texture2D puzzleImage;   // drag your cat picture here
    public int columns = 4;         // how many pieces across
    public int rows = 4;            // how many pieces down
    public float blockSpacing = 1.1f; // gap between blocks in the grid
    public bool shufflePieces = true; // mix them up so it's a real puzzle!
    public GameObject frameReference; // your own frame object - puzzle will fit inside it

    // This adds a menu item at the top of Unity: "Tools > Puzzle Grid Generator"
    [MenuItem("Tools/Puzzle Grid Generator")]
    public static void ShowWindow()
    {
        GetWindow<PuzzleGridGenerator>("Puzzle Grid Generator");
    }

    // This draws the little window with fields and a button
    void OnGUI()
    {
        GUILayout.Label("Turn any picture into a puzzle grid!", EditorStyles.boldLabel);

        puzzleImage = (Texture2D)EditorGUILayout.ObjectField("Puzzle Image", puzzleImage, typeof(Texture2D), false);
        columns = EditorGUILayout.IntField("Columns", columns);
        rows = EditorGUILayout.IntField("Rows", rows);
        blockSpacing = EditorGUILayout.FloatField("Block Spacing", blockSpacing);
        shufflePieces = EditorGUILayout.Toggle("Shuffle Pieces?", shufflePieces);
        frameReference = (GameObject)EditorGUILayout.ObjectField("Frame Object", frameReference, typeof(GameObject), true);

        GUILayout.Space(10);

        if (GUILayout.Button("Generate Puzzle Grid"))
        {
            GeneratePuzzle();
        }
    }

    void GeneratePuzzle()
    {
        if (puzzleImage == null)
        {
            Debug.LogError("Please drag in a puzzle image first!");
            return;
        }

        // Step 1: Find the path to the image so we can load its sliced pieces
        string path = AssetDatabase.GetAssetPath(puzzleImage);
        Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(path);

        // Step 2: Create an empty parent object to hold all our blocks neatly
        GameObject puzzleParent = new GameObject("Puzzle_" + puzzleImage.name);

        int blockIndex = 0;
        List<GameObject> allBlocks = new List<GameObject>();       // remember every block we make
        List<Vector3> correctHomePositions = new List<Vector3>();  // remember where each one SHOULD end up

        // Step 3: Loop through rows and columns, placing each sliced piece
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                // Find the correct sliced sprite by its expected name (e.g. "cat_0", "cat_1"...)
                string spriteName = puzzleImage.name + "_" + blockIndex;
                Sprite pieceSprite = null;

                foreach (Object asset in allAssets)
                {
                    if (asset is Sprite sprite && sprite.name == spriteName)
                    {
                        pieceSprite = sprite;
                        break;
                    }
                }

                if (pieceSprite == null)
                {
                    Debug.LogWarning("Could not find sliced piece: " + spriteName + " - did you slice the image first?");
                    blockIndex++;
                    continue;
                }

                // Step 4: Create the block GameObject
                GameObject block = new GameObject("Block_" + blockIndex);
                block.transform.parent = puzzleParent.transform;

                // Step 5: Give it the picture piece
                SpriteRenderer sr = block.AddComponent<SpriteRenderer>();
                sr.sprite = pieceSprite;
                sr.sortingOrder = 1; // makes sure dragged pieces don't hide behind others weirdly

                // Step 6: Give it a clickable bubble (collider) so we can drag it later
                block.AddComponent<BoxCollider2D>();

                // Step 6.5: Give it its "brain" so we can drag it right away!
                block.AddComponent<SimpleDrag>();

                // Step 6.6: Give it a "name tag" remembering where it truly belongs
                PuzzlePiece piece = block.AddComponent<PuzzlePiece>();
                piece.pieceIndex = blockIndex;
                piece.gridColumns = columns; // remember how wide THIS puzzle is

                // Step 7: Figure out its CORRECT home position in the finished picture
                // (we flip Y because image rows go top-to-bottom, but Unity's Y goes bottom-to-top)
                float posX = x * blockSpacing;
                float posY = -y * blockSpacing;
                Vector3 homePosition = new Vector3(posX, posY, 0);

                block.transform.position = homePosition; // start it there for now
                piece.correctPosition = homePosition;    // remember it forever on the name tag

                allBlocks.Add(block);
                correctHomePositions.Add(homePosition);

                blockIndex++;
            }
        }

        // Step 8: SHUFFLE! Give every block a random messy starting spot instead
        // but ONLY inside the area the camera can actually see (our "play frame")
        if (shufflePieces)
        {
            // Step 8a: Figure out how big the camera's view is, in world units
            Camera cam = Camera.main;
            if (cam == null)
            {
                // fall back to finding any camera in the scene if MainCamera isn't tagged
                cam = Object.FindObjectOfType<Camera>();
            }

            if (cam != null)
            {
                float camHeight = cam.orthographicSize * 2f;       // full visible height
                float camWidth = camHeight * cam.aspect;           // full visible width

                // Step 8b: Leave a little safety padding so pieces don't spawn
                // right at the very edge (half a block width, plus a bit extra)
                float paddingX = camWidth * 0.1f;
                float paddingY = camHeight * 0.1f;

                float minX = cam.transform.position.x - (camWidth / 2f) + paddingX;
                float maxX = cam.transform.position.x + (camWidth / 2f) - paddingX;
                float minY = cam.transform.position.y - (camHeight / 2f) + paddingY;
                float maxY = cam.transform.position.y + (camHeight / 2f) - paddingY;

                foreach (GameObject block in allBlocks)
                {
                    float randomX = Random.Range(minX, maxX);
                    float randomY = Random.Range(minY, maxY);
                    block.transform.position = new Vector3(randomX, randomY, 0);
                }
            }
            else
            {
                Debug.LogWarning("No camera found in scene - couldn't calculate safe shuffle area. Using default range instead.");
                foreach (GameObject block in allBlocks)
                {
                    float randomX = Random.Range(-4f, 4f);
                    float randomY = Random.Range(-4f, 4f);
                    block.transform.position = new Vector3(randomX, randomY, 0);
                }
            }
        }

        Debug.Log("Puzzle grid generated with " + blockIndex + " blocks! Check your Hierarchy.");

        // Automatically add the "fit to frame" brain so you never
        // have to manually drag it on again for future puzzles!
        ResponsivePuzzleFitter fitter = puzzleParent.GetComponent<ResponsivePuzzleFitter>();
        if (fitter == null)
        {
            fitter = puzzleParent.AddComponent<ResponsivePuzzleFitter>();
        }
        fitter.frameReference = frameReference; // connect your chosen frame automatically
    }
}