using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text;
using UnityEngine;

public enum BlockType { Empty, Blue, Gray, Purple, Red, Yellow };

public class Block
{
    public GameObject obj;
    public BlockType type;
    public int idxX;
    public int idxY;
    public bool isLocked;
};

public class MainLogic : MonoBehaviour
{
    public GameObject[] BlockPrefabs;

    private const int MatchCount = 3;
    private const int XCount = 10;
    private const int YCount = 10;
    private const int GridSize = 1;

    private Block[,] mBlocks = null;
    private Vector2 mMouseDownPos;


    // Start is called before the first frame update
    void Start()
    {
        mBlocks = new Block[XCount, YCount];
        for (int y = 0; y < YCount; y++)
        {
            for (int x = 0; x < XCount; x++)
            {
                int typeNum = Random.Range(1, BlockPrefabs.Length);
                mBlocks[x, y] = new Block();
                mBlocks[x, y].obj = GameObject.Instantiate(BlockPrefabs[typeNum] as GameObject, new Vector3(GridSize * x, GridSize * y, -1), transform.rotation) as GameObject;
                mBlocks[x, y].isLocked = false;
                mBlocks[x, y].idxX = x;
                mBlocks[x, y].idxY = y;
                mBlocks[x, y].type = (BlockType)typeNum;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        Vector2Int swipeDir = Swipe();
        if(swipeDir.magnitude > 0)
        {
            DoSwapBlocks(swipeDir);
        }
    }

    void DoSwapBlocks(Vector2Int dir)
    {
        Block firstBlock = PixelToIndex(new Vector2(mMouseDownPos.x, mMouseDownPos.y));
        Block secBlock = mBlocks[firstBlock.idxX + dir.x, firstBlock.idxY + dir.y];
        if (firstBlock.isLocked || secBlock.isLocked)
            return;

        firstBlock.isLocked = true;
        secBlock.isLocked = true;

        StartCoroutine(AnimateSwap(firstBlock, secBlock));
    }
    void DoMatchBlock(Block startBlock)
    {
        List<Block> blocks = new List<Block>();
        FindMatchedBlocks(startBlock.idxX, startBlock.idxY, startBlock.type, blocks);
        if (blocks.Count < MatchCount)
            return;

        foreach (Block block in blocks)
            block.isLocked = true;

        StartCoroutine(AnimateDestroy(blocks));
    }
    void DoEmptyBlocks(List<Block> emptyBlocks)
    {
        List<Block> createBlocks = new List<Block>();
        List<Block> linkPathBlocks = new List<Block>();
        Dictionary<int, Vector2Int> linkableBlocks = new Dictionary<int, Vector2Int>();
        DetectComboBlocks(emptyBlocks, createBlocks, linkPathBlocks, linkableBlocks);

        foreach(var block in linkableBlocks)
        {
            Vector2Int idxFrom = new Vector2Int(block.Key, block.Value.x);
            Vector2Int idxTo = new Vector2Int(block.Key, block.Value.y);
            for (int y = idxTo.y; y <= idxFrom.y; ++y)
                mBlocks[block.Key, y].isLocked = true;
            StartCoroutine(AnimateDrop(idxFrom, idxTo));
        }

        foreach (var empBlock in createBlocks)
            empBlock.isLocked = true;
        StartCoroutine(AnimateCreate(createBlocks));
    }



    IEnumerator AnimateSwap(Block blockA, Block blockB)
    {
        Vector3 posA = blockA.obj.transform.position;
        Vector3 posB = blockB.obj.transform.position;
        float time = 0;
        while (time < 1)
        {
            time += Time.deltaTime;
            blockA.obj.transform.position = Vector3.MoveTowards(blockA.obj.transform.position, posB, GridSize * Time.deltaTime);
            blockB.obj.transform.position = Vector3.MoveTowards(blockB.obj.transform.position, posA, GridSize * Time.deltaTime);
            yield return null;
        }

        SwapBlock(blockA, blockB);

        blockA.isLocked = false;
        blockB.isLocked = false;

        DoMatchBlock(blockA);
        DoMatchBlock(blockB);
    }
    IEnumerator AnimateDestroy(List<Block> blocks)
    {
        float time = 0;
        while (time < 1)
        {
            time += Time.deltaTime;
            foreach (Block block in blocks)
            {
                block.obj.transform.localScale = new Vector3(1 - time, 1 - time, 1);
            }

            yield return null;
        }

        foreach (Block block in blocks)
        {
            block.obj.transform.localScale = new Vector3(1, 1, 1);
            block.obj.SetActive(false);
            block.type = BlockType.Empty;
            block.isLocked = false;
        }

        DoEmptyBlocks(blocks);
    }
    IEnumerator AnimateCreate(List<Block> blocks)
    {
        foreach (Block block in blocks)
        {
            int typeNum = Random.Range(1, BlockPrefabs.Length);
            block.obj.SetActive(true);
            block.obj.GetComponent<SpriteRenderer>().sprite = BlockPrefabs[typeNum].GetComponent<SpriteRenderer>().sprite;
            block.type = (BlockType)typeNum;
            block.obj.transform.localScale = new Vector3(0, 0, 1);
        }

        float time = 0;
        while (time < 1)
        {
            time += Time.deltaTime;
            foreach (Block block in blocks)
            {
                block.obj.transform.localScale = new Vector3(time, time, 1);
            }

            yield return null;
        }

        foreach (Block block in blocks)
        {
            block.obj.transform.localScale = new Vector3(1, 1, 1);
            block.isLocked = false;
        }

        //foreach (Block block in blocks)
        //   DoMatchBlock(block);
    }
    IEnumerator AnimateDrop(Vector2Int idxFrom, Vector2Int idxTo)
    {
        Block blockFrom = mBlocks[idxFrom.x, idxFrom.y];
        Block blockTo = mBlocks[idxTo.x, idxTo.y];

        float time = 0;
        while (time < 1)
        {
            time += Time.deltaTime;
            blockFrom.obj.transform.position = Vector3.MoveTowards(blockFrom.obj.transform.position, blockTo.obj.transform.position, GridSize * Time.deltaTime);
            yield return null;
        }

        SwapBlock(blockFrom, blockTo);

        List<Block> emptyBlocks = new List<Block>();
        for(int i = idxTo.y; i <= idxFrom.y; ++i)
        {
            mBlocks[idxTo.x, i].isLocked = false;
            if (mBlocks[idxTo.x, i].type == BlockType.Empty)
                emptyBlocks.Add(mBlocks[idxTo.x, i]);
        }

        DoMatchBlock(mBlocks[idxTo.x, idxTo.y]);
        DoEmptyBlocks(emptyBlocks);
    }




    private Vector2Int Swipe()
    {
        if (Input.GetMouseButtonDown(0))
            mMouseDownPos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);

        if (Input.GetMouseButtonUp(0))
        {
            Vector2 _secondPressPos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            Vector2 _currentSwipe = new Vector2(_secondPressPos.x - mMouseDownPos.x, _secondPressPos.y - mMouseDownPos.y);
            _currentSwipe.Normalize();

            if (_currentSwipe.y > 0 && _currentSwipe.x > -0.5f && _currentSwipe.x < 0.5f)
                return new Vector2Int(0, 1);
            if (_currentSwipe.y < 0 && _currentSwipe.x > -0.5f && _currentSwipe.x < 0.5f)
                return new Vector2Int(0, -1);
            if (_currentSwipe.x < 0 && _currentSwipe.y > -0.5f && _currentSwipe.y < 0.5f)
                return new Vector2Int(-1, 0);
            if (_currentSwipe.x > 0 && _currentSwipe.y > -0.5f && _currentSwipe.y < 0.5f)
                return new Vector2Int(1, 0);
        }
        return new Vector2Int(0, 0);
    }
    Block PixelToIndex(Vector2 pixel)
    {
        Vector2 worldPt = Camera.main.ScreenToWorldPoint(pixel);
        worldPt.x += GridSize * 0.5f;
        worldPt.y += GridSize * 0.5f;
        int idxX = (int)(worldPt.x / GridSize);
        int idxY = (int)(worldPt.y / GridSize);
        if (idxX < 0 || XCount <= idxX || idxY < 0 || YCount <= idxY)
            return null;

        return mBlocks[idxX, idxY];
    }
    void FindMatchedBlocks(int startIdxX, int startIdxY, BlockType type, List<Block> matchedBlocks)
    {
        if (startIdxX < 0 || XCount <= startIdxX)
            return;
        if (startIdxY < 0 || YCount <= startIdxY)
            return;

        Block block = mBlocks[startIdxX, startIdxY];
        if (block.type != type || matchedBlocks.Contains(block) || block.isLocked)
            return;

        matchedBlocks.Add(block);

        FindMatchedBlocks(startIdxX, startIdxY + 1, type, matchedBlocks);
        FindMatchedBlocks(startIdxX, startIdxY - 1, type, matchedBlocks);
        FindMatchedBlocks(startIdxX + 1, startIdxY, type, matchedBlocks);
        FindMatchedBlocks(startIdxX - 1, startIdxY, type, matchedBlocks);
    }
    void DetectComboBlocks(List<Block> emptyBlocks, List<Block> createBlocks, List<Block> linkPathBlocks, Dictionary<int, Vector2Int> linkableBlocks)
    {
        List<Block> candidates = new List<Block>();
        foreach (Block block in emptyBlocks)
        {
            if (block.idxY >= YCount - 1)
                continue;

            Block upBlock = mBlocks[block.idxX, block.idxY + 1];
            if (upBlock.isLocked || upBlock.type == BlockType.Empty)
                continue;

            candidates.Add(upBlock);
        }

        foreach (Block block in candidates)
        {
            int dropIndexY = FindDropIndex(block);
            if (IsComboable(block.idxX, dropIndexY, block.type))
                linkableBlocks[block.idxX] = new Vector2Int(block.idxY, dropIndexY);
        }

        foreach (Block block in emptyBlocks)
        {
            if (linkableBlocks.ContainsKey(block.idxX))
                linkPathBlocks.Add(block);
            else
                createBlocks.Add(block);
        }
    }
    bool IsComboable(int idxX, int idxY, BlockType type)
    {
        List<Block> matchedBlocks = new List<Block>();
        FindMatchedBlocks(idxX + 1, idxY, type, matchedBlocks);
        FindMatchedBlocks(idxX - 1, idxY, type, matchedBlocks);
        FindMatchedBlocks(idxX, idxY - 1, type, matchedBlocks);
        return matchedBlocks.Count >= MatchCount - 1;
    }
    int FindDropIndex(Block block)
    {
        if (block.idxY <= 0)
            return 0;

        int curIdxY = block.idxY - 1;
        while (true)
        {
            if (curIdxY == 0)
                break;
            if (mBlocks[block.idxX, curIdxY - 1].isLocked)
                break;
            if (mBlocks[block.idxX, curIdxY - 1].type != BlockType.Empty)
                break;

            curIdxY--;
        }
        return curIdxY;
    }
    Block SetBlock(int idxX, int idxY, Block pushBlock)
    {
        Block preBlock = mBlocks[idxX, idxY];
        mBlocks[idxX, idxY] = pushBlock;
        pushBlock.idxX = idxX;
        pushBlock.idxY = idxY;
        pushBlock.obj.transform.position = new Vector3(idxX * GridSize, idxY * GridSize, -1);
        pushBlock.obj.transform.localScale = new Vector3(1, 1, 1);
        return preBlock;
    }
    void SwapBlock(Block blockA, Block blockB)
    {
        int tmpX = blockB.idxX;
        int tmpY = blockB.idxY;
        SetBlock(blockA.idxX, blockA.idxY, blockB);
        SetBlock(tmpX, tmpY, blockA);
    }
}
