using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

enum direction
{
    horiz, verti
}

public unsafe class Memory
{
    // Handle for the process heap. This handle is used in all calls to the
    // HeapXXX APIs in the methods below.
    static int ph = GetProcessHeap();
    // Private instance constructor to prevent instantiation.
    private Memory() { }
    // Allocates a memory block of the given size. The allocated memory is
    // automatically initialized to zero.
    public static void* Alloc(int size)
    {
        void* result = HeapAlloc(ph, HEAP_ZERO_MEMORY, size);
        if (result == null) throw new OutOfMemoryException();
        return result;
    }
    // Copies count bytes from src to dst. The source and destination
    // blocks are permitted to overlap.
    public static void Copy(void* src, void* dst, int count)
    {
        byte* ps = (byte*)src;
        byte* pd = (byte*)dst;
        if (ps > pd)
        {
            for (; count != 0; count--) *pd++ = *ps++;
        }
        else if (ps < pd)
        {
            for (ps += count, pd += count; count != 0; count--) *--pd = *--ps;
        }
    }
    // Frees a memory block.
    public static void Free(void* block)
    {
        if (!HeapFree(ph, 0, block)) throw new InvalidOperationException();
    }
    // Re-allocates a memory block. If the reallocation request is for a
    // larger size, the additional region of memory is automatically
    // initialized to zero.
    public static void* ReAlloc(void* block, int size)
    {
        void* result = HeapReAlloc(ph, HEAP_ZERO_MEMORY, block, size);
        if (result == null) throw new OutOfMemoryException();
        return result;
    }
    // Returns the size of a memory block.
    public static int SizeOf(void* block)
    {
        int result = HeapSize(ph, 0, block);
        if (result == -1) throw new InvalidOperationException();
        return result;
    }
    // Heap API flags
    const int HEAP_ZERO_MEMORY = 0x00000008;
    // Heap API functions
    [DllImport("kernel32")]
    static extern int GetProcessHeap();
    [DllImport("kernel32")]
    static extern void* HeapAlloc(int hHeap, int flags, int size);
    [DllImport("kernel32")]
    static extern bool HeapFree(int hHeap, int flags, void* block);
    [DllImport("kernel32")]
    static extern void* HeapReAlloc(int hHeap, int flags,
       void* block, int size);
    [DllImport("kernel32")]
    static extern int HeapSize(int hHeap, int flags, void* block);
}
unsafe class bytesprocs
{
    public static bool is_equal(byte* dst, byte* src, int len)
    {
        for (; len != 0; len--)
        {
            if ((*src) != (*dst)) return false;
            src++;
            dst++;
        }
        return true;
    }

    public static void bytescopy(byte* dst, byte* src, int len)
    {
        for (; len != 0; len--)
        {
            (*dst) = (*src);
            src++;
            dst++;
        }
    }
    public static bool has_pattern(byte* memo, byte* pattern, int lenpattern, int len)
    {
        for (; len != 0; len--)
        {
            if (bytesprocs.is_equal(memo, pattern, lenpattern)) return true;
            memo += lenpattern;
        }
        return false;
    }

    public static void push_pattern(byte* memo, byte* pattern, int lenpattern, int len)
    {
        memo += len * lenpattern;
        bytescopy(memo, pattern, lenpattern);
    }

}

class piece
{
    public direction dire;
    public int length;
    public piece()
    {
    }
    public piece(direction d, int l)
    {
        dire = d;
        length = l;
    }
    override public String ToString()
    {
        return String.Format("{0} [{1}]|", dire, length);
    }

    public bool is_in_piece(int posx, int posy, int px, int py)
    {
        if (dire == direction.horiz)
        {
            if (py != posy) return false;
            if (px < posx) return false;
            if (px >= posx + length) return false;
            return true;
        }
        if (dire == direction.verti)
        {
            if (px != posx) return false;
            if (py < posy) return false;
            if (py >= posy + length) return false;
            return true;
        }
        throw new Exception("is_in_piece");
    }
}

class zonep : piece
{
    int posx, posy;
    public zonep(direction d, int l, int x, int y)
    {
        dire = d;
        length = l;
        posx = x;
        posy = y;
    }
    override public String ToString()
    {
        return String.Format("{0} [{1}]|({2},{3})", dire, length, posx, posy);
    }

    public bool is_in_zone(int px, int py)
    {
        return is_in_piece(posx, posy, px, py);
    }

    public void start(out int px, out int py)
    {
        px = posx;
        py = posy;
    }

    public bool next(ref int px, ref int py)
    {
        if (dire == direction.horiz)
        {
            px++;
            py = posy;
            return px != posx + length;
        }
        if (dire == direction.verti)
        {
            py++;
            px = posx;
            return py != posy + length;
        }
        throw new Exception("next");
    }

    public bool overlaps(zonep azone)
    {
        int ax, ay;
        azone.start(out ax, out ay);
        do
        {
            if (is_in_zone(ax, ay)) return true;
        }
        while (azone.next(ref ax, ref ay));
        return false;
    }
}

unsafe class tracedata
{
    const int HUGELENTRACE = 400000;
    const int LENMAXHASH = 32;
    int poivechash;
    byte* arrhash;
    public int poitracehash = 0;
    byte* tracehash;

    public tracedata()
    {
        arrhash = (byte*)Memory.Alloc(LENMAXHASH);
        tracehash = (byte*)Memory.Alloc(HUGELENTRACE * LENMAXHASH);
    }
    ~tracedata()
    {
        Memory.Free(arrhash);
        Memory.Free(tracehash);
    }

    public void push_hash()
    {
        bytesprocs.push_pattern(tracehash, arrhash, LENMAXHASH, poitracehash);
        poitracehash++;
        if (poitracehash == HUGELENTRACE)
            throw new Exception("stack overflow huge trace");
    }

    public bool pos_hash_in_trace()
    {
        return bytesprocs.has_pattern(tracehash, arrhash, LENMAXHASH, poitracehash);
    }

    public void push_byte_hash(byte b)
    {
        *(arrhash + poivechash++) = b;
        //arrhash[poivechash] = b;
        //poivechash++;
        //if (poivechash == LENMAXHASH)
        //    throw new Exception("stack overflow hash");
    }

    public void makehash(int poistack_state, int lennexts, int[] lposx, int[] lposy)
    {
        byte c;
        int i;
        poivechash = 0;
        byte bpoistack_state = (byte)poistack_state;
        push_byte_hash(bpoistack_state);
        for (i = 0; i < lennexts; i++)
        {
            c = (byte)lposx[i];
            push_byte_hash(c);
            c = (byte)lposy[i];
            push_byte_hash(c);
        }
    }
}

class unblmedata
{
    tracedata trace = new tracedata();
    static void getenterzonea(piece piec, int sx, int sy, int move, out zonep zone)
    {
        direction dire = piec.dire;
        int len = piec.length;
        int newlen;
        int posx, posy;
        if (dire == direction.horiz)
        {
            if (move > 0)
            {
                posy = sy;
                if (move >= len)
                {
                    newlen = len;
                    posx = sx + move;
                }
                else
                {
                    newlen = move;
                    posx = sx + len;
                }
            }
            else
            {
                move = -move;
                posx = sx - move;
                posy = sy;
                if (move >= len)
                    newlen = len;
                else
                    newlen = move;
            }
            zone = new zonep(dire, newlen, posx, posy);
            return;
        }
        if (dire == direction.verti)
        {
            if (move > 0)
            {
                posx = sx;
                if (move >= len)
                {
                    newlen = len;
                    posy = sy + move;
                }
                else
                {
                    newlen = move;
                    posy = sy + len;
                }
            }
            else
            {
                move = -move;
                posy = sy - move;
                posx = sx;
                if (move >= len)
                    newlen = len;
                else
                    newlen = move;
            }
            zone = new zonep(dire, newlen, posx, posy);
            return;
        }
        throw new Exception("getenterzonea");
    }
    static void getenterzone(piece piec, int sx, int sy, int move, out zonep zone)
    {
        direction dire = piec.dire;
        int len = piec.length;
        int newlen = Math.Abs(move);
        int posx, posy;
        if (dire == direction.horiz)
        {
            if (move > 0)
            {
                posy = sy;
                posx = sx + len;
            }
            else
            {
                move = -move;
                posy = sy;
                posx = sx - move;
            }
            zone = new zonep(dire, newlen, posx, posy);
            return;
        }
        if (dire == direction.verti)
        {
            if (move > 0)
            {
                posx = sx;
                posy = sy + len;
            }
            else
            {
                move = -move;
                posx = sx;
                posy = sy - move;
            }
            zone = new zonep(dire, newlen, posx, posy);
            return;
        }

        throw new Exception("getenterzone");
    }
    static void getleavezonea(piece piec, int sx, int sy, int move, out zonep zone)
    {
        direction dire = piec.dire;
        int len = piec.length;
        int newlen;
        int posx, posy;
        if (dire == direction.horiz)
        {
            if (move > 0)
            {
                posx = sx;
                posy = sy;
                if (move >= len)
                    newlen = len;
                else
                    newlen = move;
            }
            else
            {
                move = -move;
                posy = sy;
                if (move >= len)
                {
                    newlen = len;
                    posx = sx;
                }
                else
                {
                    newlen = move;
                    posx = sx + len - move;
                }
            }
            zone = new zonep(dire, newlen, posx, posy);
            return;
        }
        if (dire == direction.verti)
        {
            if (move > 0)
            {
                posx = sx;
                posy = sy;
                if (move >= len)
                    newlen = len;
                else
                    newlen = move;
            }
            else
            {
                move = -move;
                posx = sx;
                if (move >= len)
                {
                    newlen = len;
                    posy = sy;
                }
                else
                {
                    newlen = move;
                    posy = sy + len - move;
                }
            }
            zone = new zonep(dire, newlen, posx, posy);
            return;
        }
        throw new Exception("getleavezonea");
    }

    static void getleavezone(piece piec, int sx, int sy, int move, out zonep zone)
    {
        direction dire = piec.dire;
        int len = piec.length;
        int newlen = Math.Abs(move);
        int posx, posy;
        if (dire == direction.horiz)
        {
            if (move > 0)
            {
                posy = sy;
                posx = sx;
            }
            else
            {
                move = -move;
                posy = sy;
                posx = sx + len - move;
            }
            zone = new zonep(dire, newlen, posx, posy);
            return;
        }
        if (dire == direction.verti)
        {
            if (move > 0)
            {
                posx = sx;
                posy = sy;
            }
            else
            {
                move = -move;
                posx = sx;
                posy = sy + len - move;
            }
            zone = new zonep(dire, newlen, posx, posy);
            return;
        }
        throw new Exception("getleavezone");
    }

    public static void testoverlaps()
    {
        direction dire = direction.horiz;
        piece pies = new piece(dire, 3);
        int posx = 0;
        int posy = 2;
        zonep zone;
        getleavezone(pies, posx, posy, -4, out zone);
        Console.WriteLine(zone);
    }

    int lenx;
    int leny;
    int[][] data;
    int lennexts;
    int[] nexts;
    int[] types;
    int lenstack;
    int poistack_state;
    int poistack_nexts;
    int poistack_moves;

    int[][] stacknexts;
    int[] stackpieces;
    int[] stackmoves;
    int[] stackpos;
    int exitx, exity;
    int colorexit;
    ////////////////////////////////////////////////////
    ////////////////////////////////////////////////////
    piece[] lpiece;
    public int[] lposx;
    public int[] lposy;
    int[][] stacklposx;
    int[][] stacklposy;
    zonep[] stackzonee;
    zonep[] stackzoneea;
    zonep[] stackzonel;
    zonep[] stackzonela;
    ////////////////////////////////////////////////////
    ////////////////////////////////////////////////////
    public int MAXDEPTH = 0;
    ////////////////////////////////////////////////////
    ////////////////////////////////////////////////////
    public DateTime startime;
    public int countmoves;
    ////////////////////////////////////////////////////
    ////////////////////////////////////////////////////
    int maxdata()
    {
        int i, j;
        int retmax = 0;
        for (i = 0; i < lenx; i++)
            for (j = 0; j < leny; j++)
                if (data[i][j] > retmax)
                    retmax = data[i][j];
        return retmax;
    }

    void alloc_mats()
    {
        int i;
        data = new int[lenx][];
        for (i = 0; i < lenx; i++)
            data[i] = new int[leny];
    }

    void alloc_nexts()
    {
        int i;
        lennexts = maxdata();
        nexts = new int[lennexts * 2];
        types = new int[lennexts];

        stacknexts = new int[lenstack][];
        for (i = 0; i < lenstack; i++)
            stacknexts[i] = new int[lennexts * 2];

        stackpieces = new int[lenstack];
        stackmoves = new int[lenstack];
        stackpos = new int[lenstack];
        stackzonee = new zonep[lenstack];
        stackzoneea = new zonep[lenstack];
        stackzonel = new zonep[lenstack];
        stackzonela = new zonep[lenstack];
    }

    void alloc_lists()
    {
        int i;
        lpiece = new piece[lennexts];
        lposx = new int[lennexts];
        lposy = new int[lennexts];
        stacklposx = new int[lenstack][];
        stacklposy = new int[lenstack][];
        for (i = 0; i < lenstack; i++)
        {
            stacklposx[i] = new int[lennexts];
            stacklposy[i] = new int[lennexts];
        }
    }

    public void simu_load()
    {
        lenx = 6;
        leny = 6;
        lenstack = 300;
        alloc_mats();
        //simu_data();
        alloc_nexts();
        alloc_lists();
        calctypes();
        printtypes();
    }
    ////////////////////////////////////////////////////
    ////////////////////////////////////////////////////

    void move_ind(int ind, int d)
    {
        piece piece = lpiece[ind];
        if (piece.dire == direction.horiz)
        {
            lposx[ind] += d;
        }
        if (piece.dire == direction.verti)
        {
            lposy[ind] += d;
        }
    }

    ////////////////////////////////////////////////////
    ////////////////////////////////////////////////////
    int ind_piece_in_pos(int[] lposx, int[] lposy, int px, int py)
    {
        int i;
        for (i = 0; i < lennexts; i++)
            if (lpiece[i].is_in_piece(lposx[i], lposy[i], px, py)) return i;
        return -1;
    }

    public void print_data(int[] lposx, int[] lposy)
    {
        int i, j;
        int k;
        for (j = 0; j < leny; j++)
        {
            for (i = 0; i < lenx; i++)
            {
                k = ind_piece_in_pos(lposx, lposy, i, j);
                if (k == -1)
                    Console.Write("  ");
                else
                    Console.Write(" {0}", (char)('A' + k));
            }
            Console.WriteLine();
        }
    }

    bool is_in_a_piece(int px, int py)
    {
        int i;
        for (i = 0; i < lennexts; i++)
            if (lpiece[i].is_in_piece(lposx[i], lposy[i], px, py)) return true;
        return false;
    }

    int nrtomarg(int posx, int posy, int dx, int dy)
    {
        int i, j;
        int cou = -1;
        i = posx;
        j = posy;
        do
        {
            i += dx;
            j += dy;
            cou++;
        }
        while (!is_afara(i, j) && !is_in_a_piece(i, j));
        return cou;
    }

    void get_margins(piece piece, int posx, int posy, out int tosta, out int toend)
    {
        if (piece.dire == direction.horiz)
        {
            tosta = nrtomarg(posx, posy, -1, 0);
            toend = nrtomarg(posx + piece.length - 1, posy, 1, 0);
            return;
        }

        if (piece.dire == direction.verti)
        {
            tosta = nrtomarg(posx, posy, 0, -1);
            toend = nrtomarg(posx, posy + piece.length - 1, 0, 1);
            return;
        }
        throw new Exception("get_margins");
    }

    void make_nexts()
    {
        int tosta, toend;
        int i;
        for (i = 0; i < lennexts; i++)
        {
            get_margins(lpiece[i], lposx[i], lposy[i], out tosta, out toend);
            nexts[2 * i] = -tosta;
            nexts[2 * i + 1] = toend;
        }
    }

    ////////////////////////////////////////////////////
    ////////////////////////////////////////////////////

    void getrect(int val, out int minx, out int miny, out int maxx, out int maxy)
    {
        int i, j;
        minx = Int32.MaxValue;
        miny = Int32.MaxValue;
        maxx = Int32.MinValue;
        maxy = Int32.MinValue;
        for (i = 0; i < lenx; i++)
            for (j = 0; j < leny; j++)
                if (data[i][j] == val)
                {
                    if (i < minx) minx = i;
                    if (i > maxx) maxx = i;
                    if (j < miny) miny = j;
                    if (j > maxy) maxy = j;
                }
    }

    void getpieceposfromval(int val, out piece piece, out int posx, out int posy)
    {
        direction dire;
        int leng;

        int minx, miny, maxx, maxy;
        getrect(val, out minx, out miny, out maxx, out maxy);
        if (minx == Int32.MaxValue) throw new Exception(String.Format("value {0} doesnt exist", val));
        if (miny == Int32.MaxValue) throw new Exception(String.Format("value {0} doesnt exist", val));
        if (maxx == Int32.MinValue) throw new Exception(String.Format("value {0} doesnt exist", val));
        if (maxy == Int32.MinValue) throw new Exception(String.Format("value {0} doesnt exist", val));
        if (minx == maxx)
        { dire = direction.verti; posx = minx; posy = miny; leng = maxy - miny + 1; piece = new piece(dire, leng); return; }
        if (miny == maxy)
        { dire = direction.horiz; posx = minx; posy = miny; leng = maxx - minx + 1; piece = new piece(dire, leng); return; }
        throw new Exception("getpieceposfromval");
    }


    bool is_afara(int x, int y)
    {
        if (x < 0) return true;
        if (y < 0) return true;
        if (x >= lenx) return true;
        if (y >= leny) return true;
        return false;
    }

    void calctypes()
    {
        int i;
        for (i = 0; i < lennexts; i++)
        {
            getpieceposfromval(i + 1, out lpiece[i], out lposx[i], out lposy[i]);
        }
    }

    void printtypes()
    {
        int i;
        for (i = 0; i < lennexts; i++)
        {
            if (types[i] == 0)
            { Console.Write("H"); continue; }
            if (types[i] == 1)
            { Console.Write("V"); continue; }
            throw new Exception(String.Format("value {0} strange error printtypes", i));
        }
        Console.WriteLine();
        for (i = 0; i < lennexts; i++)
        {
            Console.WriteLine("p[{0}] = {1} at ({2},{3})", i, lpiece[i], lposx[i], lposy[i]);
        }
        Console.WriteLine();
    }

    ////////////////////////////////////////////////////
    ////////////////////////////////////////////////////

    void repkeys()
    {
        ConsoleKeyInfo keyinfo;
        ConsoleKey key;
        int pos = 0;
        Console.WriteLine("Solution found press any key to view, or Esc to quit");
        keyinfo = Console.ReadKey(true);
        if (keyinfo.Key == ConsoleKey.Escape) return;
        while (true)
        {
            try
            {
                Console.Clear();
            }
            catch
            {
            }
            Console.WriteLine("Move no {0}", pos);
            Console.WriteLine();
            print_data(stacklposx[pos], stacklposy[pos]);
            Console.WriteLine();
            Console.WriteLine("press Esc to quit, Home/UP/DOWN arrows to navigate solution");
            keyinfo = Console.ReadKey(true);
            key = keyinfo.Key;
            switch (key)
            {
                case ConsoleKey.Home: pos = 0; break;
                case ConsoleKey.UpArrow: pos--; if (pos < 0) pos = 0; break;
                case ConsoleKey.DownArrow: pos++; if (pos >= poistack_state) pos = poistack_state - 1; break;
                case ConsoleKey.Escape: return;
            }

        }
    }

    ////////////////////////////////////////////////////
    ////////////////////////////////////////////////////

    void print_footprint(int val, direction type, int pos)
    {
        Console.WriteLine();
        Console.WriteLine("{0}({1}): {2}", val, type, pos);
    }

    void print_stack()
    {
        int i;
        for (i = 0; i < poistack_state; i++)
        {
            //Console.WriteLine("{0} :", poistack_state);
            Console.WriteLine("data no {0} :", i);
            print_data(stacklposx[i], stacklposy[i]);
            Console.WriteLine(@"test_case_step({0},{1});\\{2}", stackpieces[i], stackmoves[i], (char)('A' + stackpieces[i]));
            //Console.WriteLine();
        }

        Console.WriteLine("{0} moves.", poistack_moves);
        DateTime endetime = DateTime.Now;
        TimeSpan difftime = endetime - startime;
        Console.WriteLine("Run Time {0}", difftime);
        Console.WriteLine("recursion called {0} times", countmoves);
    }

    ////////////////////////////////////////////////////
    ////////////////////////////////////////////////////
    bool equal_vec(int[] dest, int[] src)
    {
        int i;
        for (i = 0; i < lennexts; i++)
            if (dest[i] != src[i]) return false;
        return true;
    }

    void copy_vec(int[] dest, int[] src)
    {
        int i;
        for (i = 0; i < lennexts; i++)
            dest[i] = src[i];
    }
    void copy_vec2(int[] dest, int[] src)
    {
        int i;
        for (i = 0; i < lennexts * 2; i++)
            dest[i] = src[i];
    }
    ////////////////////////////////////////////////////
    ////////////////////////////////////////////////////

    void calc_enter_leave(piece piece, int posx, int posy, int diff, out zonep zonee, out zonep zoneea, out zonep zonel, out zonep zonela)
    {
        if (diff == 0) throw new Exception("calc_enter_leave");

        getenterzone(piece, posx, posy, diff, out zonee);
        getenterzonea(piece, posx, posy, diff, out zoneea);
        getleavezone(piece, posx, posy, diff, out zonel);
        getleavezonea(piece, posx, posy, diff, out zonela);
    }

    ////////////////////////////////////////////////////
    ////////////////////////////////////////////////////
    void push_nexts()
    {
        copy_vec2(stacknexts[poistack_nexts], nexts);
        poistack_nexts++;
    }

    void push_state()
    {
        copy_vec(stacklposx[poistack_state], lposx);
        copy_vec(stacklposy[poistack_state], lposy);
        poistack_state++;
        if (poistack_state == lenstack)
        {
            print_stack();
            throw new Exception("Stack Full");
        }
    }

    void push_move(int i, int j)
    {
        zonep zonee, zonel;
        zonep zoneea, zonela;
        stackpieces[poistack_moves] = i;
        stackmoves[poistack_moves] = j;
        calc_enter_leave(lpiece[i], lposx[i], lposy[i], j, out zonee, out zoneea, out zonel, out zonela);
        stackzonee[poistack_moves] = zonee;
        stackzoneea[poistack_moves] = zoneea;
        stackzonel[poistack_moves] = zonel;
        stackzonela[poistack_moves] = zonela;
        poistack_moves++;
    }

    void pop_move()
    {
        poistack_moves--;
    }

    void pop_nexts()
    {
        poistack_nexts--;
        copy_vec2(nexts, stacknexts[poistack_nexts]);
    }

    void pop_state()
    {
        poistack_state--;
        copy_vec(lposx, stacklposx[poistack_state]);
        copy_vec(lposy, stacklposy[poistack_state]);
    }

    ////////////////////////////////////////////////////
    ////////////////////////////////////////////////////
    int first_val(int val)
    {
        int i;
        for (i = poistack_moves - 2; i >= 0; i--)
            if (stackpieces[i] == val) return i;
        return -1;
    }

    bool is_indep(int i, int j)
    {
        if (stackzoneea[i].overlaps(stackzonel[j])) return false;
        if (stackzoneea[j].overlaps(stackzonel[i])) return false;
        if (stackzonee[i].overlaps(stackzonela[j])) return false;
        if (stackzonee[j].overlaps(stackzonela[i])) return false;
        return true;
    }

    bool is_nordered()
    {
        if (poistack_moves < 2) return false;
        if (is_indep(poistack_moves - 1, poistack_moves - 2))
            return stackpieces[poistack_moves - 1] < stackpieces[poistack_moves - 2];
        return false;
    }

    bool moving_cycles()
    {
        bool foundl = false;
        bool founde = false;
        int i;
        if (poistack_moves < 3) return false;
        int val = stackpieces[poistack_moves - 1];
        int posvaldown = first_val(val);
        if (posvaldown == -1)
            return false;

        for (i = posvaldown + 1; i < poistack_moves - 1; i++)
        {
            if (stackzonela[posvaldown].overlaps(stackzonee[i]) || stackzonel[posvaldown].overlaps(stackzoneea[i]))
                foundl = true;
            if (stackzoneea[poistack_moves - 1].overlaps(stackzonel[i]) || stackzonee[poistack_moves - 1].overlaps(stackzonela[i]))
                founde = true;
        }
        if (!foundl) return true;
        if (!founde) return true;
        return false;
    }

    ////////////////////////////////////////////////////
    ////////////////////////////////////////////////////
    bool was_good_move()
    {
        if (moving_cycles()) return false;
        if (is_nordered()) return false;
        trace.makehash(poistack_moves, lennexts, lposx, lposy);
        if (trace.pos_hash_in_trace()) return false;
        trace.push_hash();
        return true;
    }

    bool data_exists_before()
    {
        //return false;
        int i;
        for (i = 0; i < poistack_state; i++)
        {
            if (equal_vec(stacklposx[i], lposx) && equal_vec(stacklposy[i], lposy))
                return true;
        }
        return false;
    }

    bool is_repeat_move(int ind)
    {
        if (poistack_moves == 0)
            return false;
        int val = stackpieces[poistack_moves - 1];
        return ind == val;
    }

    bool is_good_move(int ind, int indmove)
    {
        if (indmove == 0) return false;
        if (is_repeat_move(ind)) return false;
        return true;
    }
    ////////////////////////////////////////////////////
    ////////////////////////////////////////////////////

    bool is_final()
    {
        int tosta, toend;
        piece piece = lpiece[colorexit - 1];
        int posx = lposx[colorexit - 1];
        int posy = lposy[colorexit - 1];
        int length = piece.length;
        get_margins(piece, posx, posy, out tosta, out toend);

        if (piece.dire == direction.horiz)
        {
            if (exity != posy)
                throw new Exception("is_end");
            return posx - tosta == exitx || posx + length + toend - 1 == exitx;
        }
        if (piece.dire == direction.verti)
        {
            if (exitx != posx)
                throw new Exception("is_end");
            return posy - tosta == exity || posy + length + toend - 1 == exity;
        }
        throw new Exception("is_end");
    }
    ////////////////////////////////////////////////////
    ////////////////////////////////////////////////////

    public void stack_exec_move(int i, int j)
    {
        push_state();
        push_move(i, j);
        exec_move(i, j);
        pop_move();
        pop_state();
    }

    public void exec_move(int i, int j)
    {
        move_ind(i, j);
        if (!data_exists_before())
            if (was_good_move())
                recursion();
    }

    public void stack_make_nexts_and_make_moves()
    {
        push_nexts();
        make_nexts_and_make_moves();
        pop_nexts();
    }

    public void make_nexts_and_make_moves()
    {
        int i, j;
        make_nexts();
        for (i = 0; i < lennexts; i++)
            for (j = nexts[2 * i]; j <= nexts[2 * i + 1]; j++)
                if (is_good_move(i, j))
                    stack_exec_move(i, j);
    }

    public void recursion()
    {
        countmoves++;
        if ((countmoves % 1000000) == 0) Console.Write("+");
        if (is_final())
        {
            push_state();
            print_stack();
            repkeys();
            Environment.Exit(0);
        }
        if (MAXDEPTH != 0)
            if (poistack_state == MAXDEPTH)
                return;
        stack_make_nexts_and_make_moves();
    }

    ////////////////////////////////////////////////////
    ////////////////////////////////////////////////////
    static bool IsLetter(byte b)
    {
        if (b > (byte)'Z') return false;
        if (b < (byte)'A') return false;
        return true;
    }
    static bool IsDigit(byte b)
    {
        if (b > (byte)'9') return false;
        if (b < (byte)'0') return false;
        return true;
    }
    static byte[] GetNumbersLetters(byte[] arrbyte)
    {
        int i;
        int len;
        len = arrbyte.Length;
        List<byte> lbytes = new List<byte>();
        for (i = 0; i < len; i++)
            if (IsDigit(arrbyte[i]) || IsLetter(arrbyte[i]))
                lbytes.Add(arrbyte[i]);
        return lbytes.ToArray();
    }

    public static unblmedata GetUnblMeData(String namei)
    {
        FileStream fs = null;
        using (fs = new FileStream(namei, FileMode.Open))
        {
            byte[] arrbyte = new byte[4096];
            int nrread = fs.Read(arrbyte, 0, 4096);
            byte[] numbersdigits = GetNumbersLetters(arrbyte);
            return GetUnblMeData(numbersdigits);
        }
    }

    void setline(int lin, byte[] arrbytes)
    {
        int i;
        byte b;
        for (i = 0; i < lenx; i++)
        {
            b = arrbytes[2 + lenx * lin + i];
            if (IsLetter(b))
                data[i][lin] = b - 'A' + 1;
        }
    }

    void setmat(byte[] arrbytes)
    {
        int i;
        for (i = 0; i < leny; i++)
            setline(i, arrbytes);

    }

    void print_mat()
    {
        int i, j;
        for (j = 0; j < leny; j++)
        {
            for (i = 0; i < lenx; i++)
                if (data[i][j] == 0)
                    Console.Write("  ", data[i][j]);
                else
                    Console.Write(" {0}", (char)('A' + data[i][j] - 1));

            Console.WriteLine();
        }
    }

    static unblmedata GetUnblMeData(byte[] arrbytes)
    {
        unblmedata ret = new unblmedata();
        ret.lenx = arrbytes[0] - '0';
        ret.leny = arrbytes[1] - '0';
        ret.lenstack = 300;
        ret.alloc_mats();
        ret.setmat(arrbytes);
        ret.alloc_nexts();
        ret.alloc_lists();
        ret.calctypes();

        ret.colorexit = arrbytes[2 + ret.lenx * ret.leny] - 'A' + 1;
        ret.exitx = arrbytes[2 + ret.lenx * ret.leny + 1] - '0';
        ret.exity = arrbytes[2 + ret.lenx * ret.leny + 2] - '0';
        return ret;
    }

    void copy_vec_bytes(byte[] dest, byte[] src)
    {
        int i;
        int len = 2 * lennexts + 1;
        for (i = 0; i < len; i++)
            dest[i] = src[i];
    }

    bool equal_vec_bytes(byte[] dest, byte[] src)
    {
        int i;
        int len = 2 * lennexts + 1;
        for (i = 0; i < len; i++)
            if (dest[i] != src[i]) return false;
        return true;
    }


    void print_bytes(byte[] arrbytes)
    {
        int len;
        int i;
        len = arrbytes.Length;
        for (i = 0; i < len; i++)
        {
            Console.Write("{0} ", arrbytes[i]);
        }
        Console.WriteLine();
    }
}


class unblme
{
    static void Main(String[] arrstr)
    {
        String namefile = null;
        String strmaxrec = "50";
        int i;
        int len;
        String arg;
        String optarg;
        Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;
        len = arrstr.Length;
        for (i = 0; i < len; i++)
        {
            arg = arrstr[i];
            optarg = arg.Substring(0, 2);
            arg = arg.Substring(2);
            if (optarg == "-i")
            { namefile = arg; continue; }
            if (optarg == "-d")
            { strmaxrec = arg; continue; }
        }
        unblmedata vunblmedata = unblmedata.GetUnblMeData(namefile);
        vunblmedata.MAXDEPTH = Int32.Parse(strmaxrec);

        Console.WriteLine("MAXDEPTH={0}", vunblmedata.MAXDEPTH);

        Console.WriteLine("initial");
        vunblmedata.print_data(vunblmedata.lposx, vunblmedata.lposy);
        Console.WriteLine("starting computing...");
        vunblmedata.startime = DateTime.Now;
        vunblmedata.countmoves = 0;
        vunblmedata.recursion();

        Console.WriteLine("No sol. found. press a key...");
        Console.WriteLine("{0} moves.", vunblmedata.countmoves);
        //Console.WriteLine("is_indep called {0} times.", vunblmedata.countindeps);
        Console.ReadKey();
    }
}
//  A  B  C  D  E  F  G  H  I  J  K  L
//  0  1  2  3  4  5  6  7  8  9 10 11
