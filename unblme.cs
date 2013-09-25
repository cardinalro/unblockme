using System;
using System.Collections.Generic;
using System.IO;

enum direction
{
    horiz, verti
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

class unblmedata
{
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
    zonep[] stackzonel;
    ////////////////////////////////////////////////////
    ////////////////////////////////////////////////////
    public int MAXDEPTH = 0;
    public int LENPURP = 4;
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
        stackpos = new int[lenstack];
        stackzonee = new zonep[lenstack];
        stackzonel = new zonep[lenstack];
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
            print_data(stacklposx[i], stacklposy[i]);
            Console.WriteLine();
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

    void calc_enter_leave(piece piece, int posx, int posy, int diff, out zonep zonee, out zonep zonel)
    {
        int newposxe = 0;
        int newposye = 0;
        int newposxl = 0;
        int newposyl = 0;
        direction dire = piece.dire;
        if (diff == 0) throw new Exception("calc_enter_leave");

        if (piece.dire == direction.horiz)
        {
            newposye = posy;
            newposyl = posy;
            if (diff > 0)
            {
                newposxe = posx + piece.length;
                newposxl = posx;
            }
            else
            {
                newposxe = posx + diff;
                newposxl = posx + piece.length + diff;
            }
        }
        if (piece.dire == direction.verti)
        {
            newposxe = posx;
            newposxl = posx;
            if (diff > 0)
            {
                newposye = posy + piece.length;
                newposyl = posy;
            }
            else
            {
                newposye = posy + diff;
                newposyl = posy + piece.length + diff;
            }
        }
        zonee = new zonep(dire, Math.Abs(diff), newposxe, newposye);
        zonel = new zonep(dire, Math.Abs(diff), newposxl, newposyl);
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
        stackpieces[poistack_moves] = i;
        calc_enter_leave(lpiece[i], lposx[i], lposy[i], j, out zonee, out zonel);
        stackzonee[poistack_moves] = zonee;
        stackzonel[poistack_moves] = zonel;
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

    bool moving_nopurpose()
    {
        int depth = LENPURP;
        int i;
        if (poistack_moves < depth + 1) return false;
        int val = stackpieces[poistack_moves - 1];
        int posvaldown = poistack_moves - depth - 2;
        for (i = posvaldown + 1; i < poistack_moves - 1; i++)
        {
            if (stackzonee[poistack_moves - 1].overlaps(stackzonel[i])) return false;
        }
        return true;
    }

    bool moving_cycles1()
    {
        bool foundl = false;
        bool founde = false;
        int posfoundl = -1;
        int posfounde = -1;
        int i;
        if (poistack_moves < 3) return false;
        int val = stackpieces[poistack_moves - 1];
        int posvaldown = first_val(val);
        if (posvaldown == -1)
            return false;
        for (i = posvaldown + 1; i < poistack_moves - 1; i++)
        {
            if (stackzonel[posvaldown].overlaps(stackzonee[i])) { foundl = true; posfoundl = i; }
            if (stackzonee[poistack_moves - 1].overlaps(stackzonel[i])) { founde = true; posfounde = i; }
        }
        if (!foundl) return true;
        if (!founde) return true;
        if (posfoundl > posfounde)
            return true;
        return false;
    }

    bool moving_cycles()
    {
        bool foundl = false;
        bool founde = false;
        int minposfoundl = Int32.MaxValue;
        int minposfounde = Int32.MaxValue;
        int maxposfoundl = -1;
        int maxposfounde = -1;
        int i;
        if (poistack_moves < 3) return false;
        int val = stackpieces[poistack_moves - 1];
        int posvaldown = first_val(val);
        if (posvaldown == -1)
            return false;
        for (i = posvaldown + 1; i < poistack_moves - 1; i++)
        {
            if (stackzonel[posvaldown].overlaps(stackzonee[i]))
            {
                foundl = true;
                if (i < minposfoundl)
                    minposfoundl = i;
                if (i > maxposfoundl)
                    maxposfoundl = i;
            }
            if (stackzonee[poistack_moves - 1].overlaps(stackzonel[i]))
            {
                founde = true;
                if (i < minposfounde)
                    minposfounde = i;
                if (i > maxposfounde)
                    maxposfounde = i;
            }
        }
        if (!foundl) return true;
        if (!founde) return true;

        //if (minposfoundl > maxposfounde)
        //    return true;

        if (maxposfoundl > maxposfounde)
            return true;
        if (minposfoundl > minposfounde)
            return true;

        return false;
    }

    ////////////////////////////////////////////////////
    ////////////////////////////////////////////////////
    bool was_good_move()
    {
        if (moving_cycles()) return false;
        if (moving_nopurpose()) return false;
        return true;
    }

    bool data_exists_before()
    {
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
    bool is_final_old()
    {
        return lpiece[colorexit - 1].is_in_piece(lposx[colorexit - 1], lposy[colorexit - 1], exitx, exity);
    }

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
        if (is_final())
        {
            push_state();
            print_stack();
            repkeys();
            Environment.Exit(0);
        }
        if (MAXDEPTH != 0)
            if (poistack_state == MAXDEPTH) return;//some limitation
        stack_make_nexts_and_make_moves();
    }

    public void recursion1()
    {
        int i, j;
        if (is_final()) { push_state(); print_stack(); repkeys(); Environment.Exit(0); }
        if (MAXDEPTH != 0)
            if (poistack_state == MAXDEPTH) return;//some limitation
        push_nexts();
        make_nexts();
        for (i = 0; i < lennexts; i++)
            for (j = nexts[2 * i]; j <= nexts[2 * i + 1]; j++)
                if (is_good_move(i, j))
                {
                    push_state();
                    push_move(i, j);
                    move_ind(i, j);
                    if (!data_exists_before())
                        if (was_good_move())
                            recursion1();
                    pop_move();
                    pop_state();
                }
        pop_nexts();
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

}

class unblme
{
    static void Main(String[] arrstr)
    {
        String namefile = arrstr[0];
        String strmaxrec = arrstr[1];
        String strmaxpurp = arrstr[2];

        unblmedata vunblmedata = unblmedata.GetUnblMeData(namefile);
        vunblmedata.MAXDEPTH = Int32.Parse(strmaxrec);
        vunblmedata.LENPURP = Int32.Parse(strmaxpurp);
        Console.WriteLine("MAXDEPTH={0}", vunblmedata.MAXDEPTH);
        Console.WriteLine("LENPURP={0}", vunblmedata.LENPURP);

        Console.WriteLine("initial");
        vunblmedata.print_data(vunblmedata.lposx, vunblmedata.lposy);
        Console.WriteLine("starting computing...");
        vunblmedata.startime = DateTime.Now;
        vunblmedata.countmoves = 0;
        vunblmedata.recursion();
        Console.WriteLine("No sol. found. press a key...");
        Console.ReadKey();
    }
}
//  A  B  C  D  E  F  G  H  I  J  K  L
//  1  2  3  4  5  6  7  8  9 10 11 12

