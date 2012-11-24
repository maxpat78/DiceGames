using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Windows.Forms;

// Per essere compatibile con .NET 2.0, Tuple diventa questa struttura...
public struct Quintupla {
    public int Punti;
    public int Dadi;
    public int Zylk;
    public string Nome;
    public string Data;

    public Quintupla(int a, int b, int c, string y, string z)
    {
        Punti = a;
        Dadi = b;
        Zylk = c;
        Nome = z;
        Data = y;
    }
    public static int ComparaQuintupla(Quintupla a, Quintupla b)
    {
        if (a.Punti < b.Punti)
            return -1;
        if (a.Punti > b.Punti)
            return 1;

        if (a.Dadi < b.Dadi)
            return -1;
        if (a.Dadi > b.Dadi)
            return 1;

        if (a.Zylk < b.Zylk)
            return -1;
        if (a.Zylk > b.Zylk)
            return 1;

        return 0;
    }
};

namespace Zylk
{
    public partial class ZylkDialog : Form
    {
        int[] Dadi, dadiUsati;
        int puntiMano, puntiSalvati, puntiAccumulati, ZYLK;
        int totDadiTirati, totZylk;
        bool inizioGioco;
        int inizioMano;
        Random rng = new Random();
        List<Label> colonnaPunti = new List<Label>();
        List<Quintupla> tabellaPunteggi = new List<Quintupla>(); // compatibile .NET 2.0
        string nomeGiocatore = "";
        string filePunti = "zylk.txt";
        Timer tm = new Timer();
        int animCount;
        int[] animRow;

        public ZylkDialog()
        {
            InitializeComponent();
        }

        private void nuovoGioco()
        {
            totDadiTirati = totZylk = ZYLK = puntiAccumulati = puntiSalvati = 0;
            resetDadiPuntiTiri();
            inizioGioco = true;
            aggiornaTabellone();
        }

        private void resetDadiPuntiTiri()
        {
            Dadi = new int[] { -1, -1, -1, -1, -1, -1 };
            dadiUsati = new int[] { 0, 0, 0, 0, 0, 0 };
            puntiMano = puntiAccumulati = 0;
            inizioMano = 0;
            button1.Enabled = true; // Tira
            button5.Enabled = false; // Salva
        }
		
        private void caricaPunti()
        {
            if (!File.Exists(filePunti))
                return;

            System.IO.StreamReader sr = new StreamReader(filePunti);
            string riga;

            while ( (riga=sr.ReadLine()) != null )
            {
                string[] s = riga.Split(';');
                // punti;data;nome
                tabellaPunteggi.Add(new Quintupla(int.Parse(s[0]), int.Parse(s[1]), int.Parse(s[2]), s[3], s[4]));
            }
        }

        private void salvaPunti()
        {
            if (textBox1.Text == "")
                textBox1.Text = "???";

            // punti;dadi;zylk;data;nome
            tabellaPunteggi.Add(new Quintupla(puntiSalvati, totDadiTirati, totZylk, DateTime.Now.ToShortDateString(), textBox1.Text));
            tabellaPunteggi.Sort(Quintupla.ComparaQuintupla);
            tabellaPunteggi.Reverse();

            if (tabellaPunteggi.Count > 10)
                tabellaPunteggi.RemoveRange(10, tabellaPunteggi.Count - 10);

            StreamWriter sw = new StreamWriter(filePunti);

            foreach (Quintupla t in tabellaPunteggi)
            {
                sw.WriteLine(puntiSalvati.ToString() + ";"
                    + totDadiTirati.ToString() + ";"
                    + totZylk.ToString() + ";"
                    + t.Data + ";" + t.Nome);
            }

            sw.Close();
        }

        private void animaDadi()
        {
            animCount = 0;

            for (int i = 0; i < animRow.Length; i++)
            {
                if (Dadi[i] > 0) // il massimo num. di giri è calcolato sui dadi che devono girare!
                    continue;
                animRow[i] = rng.Next(12, 40); // num. di giri del singolo dado
                animCount = Math.Max(animCount, animRow[i]); // numero massimo di giri
            }

            tm.Start();
        }

        private void aggiornaTabellone()
        {
            string s;
            s = string.Format("Punti selezionati = {0}\nPunti accumulati = {1}\nPunti salvati = {2}", puntiMano, puntiMano + puntiAccumulati, puntiSalvati);
            label2.Text = s;

            if (puntiMano + puntiAccumulati > 299)
                button5.Enabled = true;

            label4.Text = string.Format("Statistiche: {0} dadi tirati, {1} zylk ({2} di seguito).", totDadiTirati, totZylk, ZYLK);
        }

        private void aggiornaDadi()
        {
            Bitmap[] bmp = { Properties.Resources.D1,
                           Properties.Resources.D2,
                           Properties.Resources.D3,
                           Properties.Resources.D4,
                           Properties.Resources.D5,
                           Properties.Resources.D6,
                           Properties.Resources.D1f,
                           Properties.Resources.D2f,
                           Properties.Resources.D3f,
                           Properties.Resources.D4f,
                           Properties.Resources.D5f,
                           Properties.Resources.D6f };

            PictureBox[] pbx = { pictureBox1, pictureBox2, pictureBox3,
                                 pictureBox4, pictureBox5, pictureBox7 };

            for (int i = 0; i < Dadi.Length; i++)
            {
                int j = Dadi[i];
                if (j < 0)
                    pbx[i].Image = bmp[j * -1 - 1];
                else
                    pbx[i].Image = bmp[j - 1 + 6];
            }
        }

        private int[] trovaMultipli(int[] dadi, int n)
        {
            int[] multipli = new int[3];
            int j = 0;

            for (int i = dadi.Length - 1; i >= 0; i--)
                if (dadi[i] >= n)
                    multipli[j++] = i+1; // multiplo più alto

            return multipli;
        }

        private bool trovaScala(int[] dadi)
        {
            for (int i = 1; i < Dadi.Length; i++)
                if (dadi[i] == 0)
                    return false;
            return true;
        }

        private int puntiUnoCinque(int faccia, int[] dadiPerValore)
        {
            if (faccia == 1)
                return dadiPerValore[4] * 51; // punti + n° dadi usati
            if (faccia == 5)
                return dadiPerValore[0] * 101;
            return dadiPerValore[0] * 101 + dadiPerValore[4] * 51;
        }

        private int contaDadiUsati()
        {
            int tot = 0;

            for (int i = 0; i < Dadi.Length; i++)
                if (Dadi[i] > 0 && dadiUsati[i] == 0)
                    tot++;

            return tot;
        }

        private int calcolaPuntiMano(bool Tutti = false)
        {
            //Dadi = new int[] { 5,5,5,5,5,6 };

            int[] dadiPerValore = new int[6];

            for (int i = 0; i < Dadi.Length; i++)
            {
                if (dadiUsati[i] > 0) continue; // esclude i dadi salvati
                if (Dadi[i] < 0 && !Tutti)  // esclude i dadi da rilanciare (di regola)
                    continue;
                dadiPerValore[Math.Abs(Dadi[i]) - 1]++;
            }

            int faccia;

            // Scala 1-6
            if (trovaScala(dadiPerValore))
                return 3006; // punti + n° dadi usati per il calcolo
            // Sestina
            faccia = trovaMultipli(dadiPerValore, 6)[0];
            if (faccia > 0)
                if (faccia != 1)
                    return faccia * 800 + 6;
                else
                    return faccia * 8000 + 6;
            // Cinquina
            faccia = trovaMultipli(dadiPerValore, 5)[0];
            if (faccia > 0)
                if (faccia != 1)
                    return faccia * 400 + puntiUnoCinque(faccia, dadiPerValore) + 5;
                else
                    return faccia * 4000 + puntiUnoCinque(faccia, dadiPerValore) + 5;
            // Quaterna
            faccia = trovaMultipli(dadiPerValore, 4)[0];
            if (faccia > 0)
            {
                int coppia = trovaMultipli(dadiPerValore, 2)[1];

                if (coppia == 0)
                    if (faccia != 1)
                        return faccia * 200 + puntiUnoCinque(faccia, dadiPerValore) + 4;
                    else
                        return faccia * 2000 + puntiUnoCinque(faccia, dadiPerValore) + 4;
                if (faccia != 1)
                    return 1506; // 3 coppie > quaterna (2-6)
                else
                    return 2006; // quaterna di 1
            }
            // Tris
            int[] Tris = trovaMultipli(dadiPerValore, 3);
            if (Tris[1] > 0) // 2 Tris
            {
                if (Tris[0] == 1)
                    return 1000 + Tris[1] * 100 + 6;
                if (Tris[1] == 1)
                    return Tris[0] * 100 + 1000 + 6;
                return Tris[0] * 100 + Tris[1] * 100 + 6;
            }
            faccia = Tris[0]; // 1 Tris
            if (faccia > 0)
                if (faccia != 1)
                    return faccia * 100 + puntiUnoCinque(faccia, dadiPerValore) + 3;
                else
                    return faccia * 1000 + puntiUnoCinque(faccia, dadiPerValore) + 3;
            // Tripla coppia
            if (trovaMultipli(dadiPerValore, 2)[2] > 0)
                return 1506;

            // 1 e 5 vanno computati per ultimi, se non inseriti in altre combinazioni
            // di maggior valore
            return puntiUnoCinque(0, dadiPerValore);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            nuovoGioco();
            caricaPunti();
            tm.Tick += new EventHandler(tm_Tick);
            tm.Interval = 50;
            animRow = new int[Dadi.Length];
        }

        void tm_Tick(object sender, EventArgs e)
        {
            if (--animCount < 0)
            {
                tm.Stop();
                if (calcolaPuntiMano(true) == 0)
                {
                    label2.Text = "ZYLK!";

                    if (inizioMano < 2)
                    {
                        resetDadiPuntiTiri();
                        puntiAccumulati = 500; // bonus per ZYLK "servito" a inizio turno
                        inizioMano = 1;
                    }
                    else
                    {
                        totZylk++;
                        if (++ZYLK == 3)
                        {
                            label2.Text = "TERZO ZYLK CONSECUTIVO!!!";
                            puntiSalvati -= 500;
                            ZYLK = 0;
                        }
                        resetDadiPuntiTiri();
                    }
                }
                return;
            }

            for (int i = 0; i < Dadi.Length; i++)
                if (Dadi[i] < 0 && animRow[i]-- > 0)
                    Dadi[i] = -1 * rng.Next(1, 7);
            aggiornaDadi();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            inizioMano++;

            // Il nuovo tiro fissa i dadi usati per il punteggio
            // e accumula i punti realizzati
            if (!inizioGioco)
            {
                puntiAccumulati += puntiMano;
                puntiMano = 0;

                for (int i = 0; i < Dadi.Length; i++)
                    if (Dadi[i] > 0)
                        dadiUsati[i] = 1;
                aggiornaTabellone();
            }

            inizioGioco = false;

            bool nessunRilancio = true;

            for (int i = 0; i < Dadi.Length; i++)
                if (Dadi[i] < 0)
                {
                    totDadiTirati++;
                    nessunRilancio = false;
                }
            // Se premuto, vuol dire che i dadi sono stati utilizzati
            // tutti per segnare punti
            if (nessunRilancio)
            {
                Dadi = new int[] { -1, -1, -1, -1, -1, -1 };
                dadiUsati = new int[] { 0, 0, 0, 0, 0, 0 };
                totDadiTirati += 6;
            }
            animaDadi();
            aggiornaTabellone();
            button1.Enabled = false;
        }

        private void pictureBox_Click(object sender, EventArgs e)
        {
            List<PictureBox> pbx = new List<PictureBox>{ pictureBox1, pictureBox2, pictureBox3,
                                 pictureBox4, pictureBox5, pictureBox7 };

            int i = pbx.FindIndex(
                delegate(PictureBox pbd)
                {
                    return (PictureBox)sender == pbd;
                });

            if (i < 0 || inizioGioco || dadiUsati[i] > 0 || calcolaPuntiMano(true) == 0)
                return;
            Dadi[i] *= -1;

            aggiornaDadi();
            
            puntiMano = calcolaPuntiMano();
            int dadiusati = puntiMano % 10;
            puntiMano -= dadiusati;
            aggiornaTabellone();

            if (puntiMano > 0) // sblocca solo se si salvano dadi utili
                button1.Enabled = true;
            else
                button1.Enabled = false;

            if (dadiusati != contaDadiUsati()) // se sono selezionati dadi inutili...
                button1.Enabled = false;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            nuovoGioco();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Form fm = new Form1();
            fm.Show();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            nomeGiocatore = textBox1.Text;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Form fm = new Form2();
            int y = 5, Height = 20;

            for (int i = 0; i < tabellaPunteggi.Count; i++)
            {
                // Posizione
                Label la1 = new Label();
                la1.Text = (i+1).ToString();
                la1.Location = new Point(10, y);
                la1.Size = new Size(25, Height);

                // Nome 
                Label la2 = new Label();
                la2.Text = tabellaPunteggi[i].Nome;
                la2.Location = new Point((la1.Location.X + la1.Size.Width - 1), y); 
                la2.Size = new Size(120, Height);

                // Data
                Label la3 = new Label();
                la3.Text = tabellaPunteggi[i].Data;
                la3.Location = new Point((la2.Location.X + la2.Size.Width - 1), y);
                la3.Size = new Size(75, Height);

                // Punti
                Label la4 = new Label();
                la4.Text = tabellaPunteggi[i].Punti.ToString();
                la4.Location = new Point((la3.Location.X + la3.Size.Width - 1), y);
                la4.Size = new Size(75, Height);
                la4.TextAlign = ContentAlignment.TopRight;

                // Dadi
                Label la5 = new Label();
                la5.Text = tabellaPunteggi[i].Dadi.ToString();
                la5.Location = new Point((la4.Location.X + la4.Size.Width - 1), y);
                la5.Size = new Size(50, Height);
                la5.TextAlign = ContentAlignment.TopRight;

                // Zylk
                Label la6 = new Label();
                la6.Text = tabellaPunteggi[i].Zylk.ToString();
                la6.Location = new Point((la5.Location.X + la5.Size.Width - 1), y);
                la6.Size = new Size(25, Height);
                la6.TextAlign = ContentAlignment.TopRight;

                la1.BackColor = la2.BackColor = la3.BackColor = la4.BackColor = la5.BackColor = la6.BackColor = Color.White;
                la1.BorderStyle = la2.BorderStyle = la3.BorderStyle = la4.BorderStyle = la5.BorderStyle = la6.BorderStyle = BorderStyle.FixedSingle;
   
                fm.Controls.Add(la1);
                fm.Controls.Add(la2);
                fm.Controls.Add(la3);
                fm.Controls.Add(la4);
                fm.Controls.Add(la5);
                fm.Controls.Add(la6);
                y += Height - 1;
            }
            fm.Show();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            puntiSalvati += puntiAccumulati + puntiMano;
            ZYLK = puntiAccumulati = puntiMano = 0;
            inizioGioco = true;
            resetDadiPuntiTiri();
            aggiornaTabellone();
            button5.Enabled = false;
            if (puntiSalvati > 9999)
            {
                salvaPunti();
                nuovoGioco();
            }
        }
    }
}

