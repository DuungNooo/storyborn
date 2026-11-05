using System.Collections.Generic;
using UnityEngine;

namespace Storyborn
{
    // Tiny generated placeholders: every pixel and palette is easy to replace later.
    // Hero source PNGs live in Resources/StorybornHero and remain directly editable.
    public static class ForestArt
    {
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        static readonly List<Texture2D> OwnedTextures = new List<Texture2D>();
        const string Outline="253E39", Cream="FFF0C3", Leaf="4E9257", LightLeaf="91C76B", Wood="8A603E";

        public static Sprite Get(string kind)
        {
            Sprite existing;
            if(Cache.TryGetValue(kind,out existing) && existing!=null) return existing;
            if(kind.StartsWith("hero_"))
            {
                string file=kind=="hero_left"?"01_left_run":kind=="hero_right"?"02_right_run":kind=="hero_jump"?"03_jump":kind=="hero_attack"?"04_attack":"05_idle";
                Sprite source=Resources.Load<Sprite>("StorybornHero/"+file);
                if(source!=null)
                {
                    Texture2D texture=source.texture;
                    // Readable textures are set in the provided .meta; guard third-party replacements.
                    if(texture.isReadable)
                    {
                        Color32[] pixels=texture.GetPixels32();
                        int minX=texture.width,minY=texture.height,maxX=-1,maxY=-1;
                        for(int y=0;y<texture.height;y++) for(int x=0;x<texture.width;x++)
                            if(pixels[y*texture.width+x].a>127)
                            { minX=Mathf.Min(minX,x);minY=Mathf.Min(minY,y);maxX=Mathf.Max(maxX,x);maxY=Mathf.Max(maxY,y); }
                        if(maxX>=minX)
                        {
                            int height=maxY-minY+1;
                            existing=Sprite.Create(texture,new Rect(minX,minY,maxX-minX+1,height),Vector2.one*.5f,height);
                            existing.name=kind;Cache[kind]=existing;return existing;
                        }
                    }
                    Cache[kind]=source;return source;
                }
            }
            Canvas c=new Canvas(32,kind=="tree"?48:32);
            switch(kind)
            {
                case "gingerbread":
                    c.Ellipse(16,23,7,7,Outline);c.Ellipse(16,23,6,6,"C88A51");
                    c.Box(10,8,12,14,Outline);c.Box(11,9,10,14,"B87842");
                    c.Box(3,15,9,5,Outline);c.Box(4,16,8,3,"C88A51");
                    c.Box(20,15,9,5,Outline);c.Box(20,16,8,3,"C88A51");
                    c.Box(9,3,6,8,Outline);c.Box(17,3,6,8,Outline);
                    c.Box(10,4,4,7,"C88A51");c.Box(18,4,4,7,"C88A51");
                    c.Box(12,23,2,2,Cream);c.Box(19,23,2,2,Cream);
                    c.Box(13,19,7,1,Cream);c.Box(15,14,2,2,"DA5E5C");c.Box(15,10,2,2,"DA5E5C");
                    break;
                case "dwarf":
                    c.Box(8,2,7,5,Outline);c.Box(18,2,7,5,Outline);c.Box(9,7,15,12,"526979");
                    c.Box(8,9,17,2,Wood);c.Ellipse(16,23,7,6,Outline);c.Ellipse(16,23,6,5,"E8B88C");
                    c.Triangle(8,27,16,32,24,27,"B5534D");c.Box(8,26,17,2,"D27963");
                    c.Box(12,23,2,2,Outline);c.Box(20,23,2,2,Outline);c.Triangle(10,21,22,21,16,14,Cream);
                    c.Line(25,7,26,27,Wood,2);c.Line(20,27,31,23,"B6C8C2",2);
                    break;
                case "apple":
                    c.Ellipse(12,14,9,10,Outline);c.Ellipse(21,14,8,10,Outline);
                    c.Ellipse(12,15,8,9,"D15358");c.Ellipse(21,15,7,9,"B83948");
                    c.Box(15,24,2,6,Wood);c.Ellipse(22,27,5,2,Leaf);
                    c.Box(7,17,3,5,"F49A81");c.Box(11,13,2,2,Cream);c.Box(22,13,2,2,Cream);
                    c.Box(14,8,6,2,Outline);break;
                case "house":
                    c.Box(4,2,25,18,Outline);c.Box(5,3,23,17,"D8AA72");
                    c.Triangle(1,19,16,32,31,19,Outline);c.Triangle(3,20,16,30,29,20,"CE6D73");
                    c.Line(3,20,16,30,Cream,2);c.Line(16,30,29,20,Cream,2);
                    c.Box(12,3,8,12,Outline);c.Box(13,3,6,11,"79503C");
                    c.Box(7,12,4,5,"97C9C7");c.Box(23,12,4,5,"97C9C7");
                    c.Box(6,5,3,3,"E58A9A");c.Box(24,5,3,3,"E58A9A");
                    c.Box(7,20,3,3,"E9C567");c.Box(15,24,3,3,"A9C87C");c.Box(22,20,3,3,"E9C567");break;
                case "beast":
                    c.Ellipse(16,17,12,11,Outline);c.Ellipse(16,17,11,10,"745E61");
                    c.Box(4,1,8,9,Outline);c.Box(21,1,8,9,Outline);c.Box(5,2,6,7,"9B7971");c.Box(22,2,6,7,"9B7971");
                    c.Ellipse(16,25,10,7,"A17B69");c.Triangle(5,27,7,32,12,28,Cream);c.Triangle(21,28,25,32,28,27,Cream);
                    c.Box(10,26,3,2,"F1CF72");c.Box(21,26,3,2,"F1CF72");
                    c.Box(13,20,8,4,Outline);c.Box(14,22,2,2,Cream);c.Box(19,22,2,2,Cream);
                    c.Box(1,10,6,10,"8C6B64");c.Box(26,10,6,10,"5B4750");
                    c.Box(1,9,2,3,Cream);c.Box(5,9,2,3,Cream);c.Box(26,9,2,3,Cream);c.Box(30,9,2,3,Cream);
                    c.Box(11,10,12,4,"4E657C");break;
                case "tree":
                    c.Box(12,0,9,31,Outline);c.Box(14,0,5,31,Wood);c.Box(14,0,2,31,"B48957");
                    c.Line(16,17,8,28,Wood,3);c.Line(18,22,26,32,Wood,3);
                    c.Ellipse(10,32,10,11,Outline);c.Ellipse(23,33,9,11,Outline);c.Ellipse(17,39,11,9,Outline);
                    c.Ellipse(10,33,9,10,Leaf);c.Ellipse(23,34,8,10,"3C794C");c.Ellipse(17,40,10,8,Leaf);
                    c.Ellipse(10,37,6,5,LightLeaf);c.Ellipse(18,43,5,3,LightLeaf);break;
                case "bush":
                    c.Ellipse(7,8,7,6,Outline);c.Ellipse(16,10,9,9,Outline);c.Ellipse(25,8,7,6,Outline);
                    c.Ellipse(7,9,6,5,Leaf);c.Ellipse(16,11,8,8,Leaf);c.Ellipse(25,9,6,5,Leaf);
                    c.Box(8,12,4,3,LightLeaf);c.Box(15,15,5,3,LightLeaf);c.Box(21,8,2,2,"E7ADBA");break;
                case "ground":
                    c.Box(0,0,32,32,"795C43");c.Box(0,25,32,7,Leaf);c.Box(0,29,32,3,LightLeaf);
                    c.Box(3,17,8,3,"9E7D53");c.Box(18,9,10,3,"9E7D53");c.Box(2,4,5,3,"544C3C");
                    c.Box(23,24,4,3,"344D36");c.Box(8,24,3,3,"344D36");break;
                case "bow":
                    // Rainbow is a physical bow: curved colored limbs and a taut pale string.
                    c.Line(10,3,17,8,Outline,3);c.Line(17,8,22,15,Outline,3);
                    c.Line(22,15,18,23,Outline,3);c.Line(18,23,10,29,Outline,3);
                    c.Line(11,4,18,9,"E78867",2);c.Line(18,9,23,15,"EBC66C",2);
                    c.Line(23,15,19,23,"7AB681",2);c.Line(19,23,11,29,"779CCA",2);
                    c.Line(10,4,10,29,Cream,1);c.Box(20,13,4,5,Wood);
                    c.Line(4,16,28,16,Cream,1);c.Triangle(25,19,31,16,25,13,"B8E1D6");
                    break;
                case "gate":
                    c.Box(3,0,5,25,Outline);c.Box(24,0,5,25,Outline);c.Box(4,0,3,25,Wood);c.Box(25,0,3,25,Wood);
                    c.Box(5,25,22,5,Wood);c.Box(6,27,20,2,"B48957");
                    c.Ellipse(7,27,6,4,Leaf);c.Ellipse(24,26,6,4,Leaf);
                    c.Box(10,11,12,3,"A8E5CD");c.Triangle(17,18,24,12,17,6,"A8E5CD");break;
                default:
                    c.Box(10,2,5,11,Outline);c.Box(18,2,5,11,Outline);c.Box(9,11,15,12,"647A9A");
                    c.Box(9,22,15,8,"E7BD9A");c.Box(8,26,17,6,"303641");c.Box(8,21,5,8,"303641");
                    c.Box(12,20,14,3,Cream);c.Box(7,14,4,8,"E7BD9A");c.Box(23,14,4,8,"E7BD9A");break;
            }
            Texture2D tex=c.Finish();OwnedTextures.Add(tex);
            existing=Sprite.Create(tex,new Rect(0,0,c.Width,c.Height),Vector2.one*.5f,32);
            existing.name=kind;Cache[kind]=existing;return existing;
        }

        sealed class Canvas
        {
            public readonly int Width,Height;
            readonly Color[] pixels;
            public Canvas(int width,int height) { Width=width;Height=height;pixels=new Color[width*height]; }
            Color Parse(string hex) { Color c;ColorUtility.TryParseHtmlString("#"+hex,out c);return c; }
            void Pixel(int x,int y,Color color) { if(x>=0 && y>=0 && x<Width && y<Height)pixels[y*Width+x]=color; }
            public void Box(int x,int y,int width,int height,string hex)
            { Color c=Parse(hex);for(int yy=y;yy<y+height;yy++)for(int xx=x;xx<x+width;xx++)Pixel(xx,yy,c); }
            public void Ellipse(int cx,int cy,int rx,int ry,string hex)
            { Color c=Parse(hex);for(int y=cy-ry;y<=cy+ry;y++)for(int x=cx-rx;x<=cx+rx;x++)if((x-cx)*(x-cx)/(float)(rx*rx)+(y-cy)*(y-cy)/(float)(ry*ry)<=1)Pixel(x,y,c); }
            public void Line(int x0,int y0,int x1,int y1,string hex,int width)
            { int steps=Mathf.Max(Mathf.Abs(x1-x0),Mathf.Abs(y1-y0));for(int i=0;i<=steps;i++){float t=steps==0?0:(float)i/steps;Box(Mathf.RoundToInt(Mathf.Lerp(x0,x1,t)),Mathf.RoundToInt(Mathf.Lerp(y0,y1,t)),width,width,hex);} }
            public void Triangle(int ax,int ay,int bx,int by,int cx,int cy,string hex)
            {
                Color c=Parse(hex);int minX=Mathf.Min(ax,Mathf.Min(bx,cx)),maxX=Mathf.Max(ax,Mathf.Max(bx,cx));
                int minY=Mathf.Min(ay,Mathf.Min(by,cy)),maxY=Mathf.Max(ay,Mathf.Max(by,cy));
                for(int y=minY;y<=maxY;y++)for(int x=minX;x<=maxX;x++)
                { int a=(x-bx)*(ay-by)-(ax-bx)*(y-by),b=(x-cx)*(by-cy)-(bx-cx)*(y-cy),d=(x-ax)*(cy-ay)-(cx-ax)*(y-ay);if((a>=0&&b>=0&&d>=0)||(a<=0&&b<=0&&d<=0))Pixel(x,y,c); }
            }
            public Texture2D Finish()
            { var tex=new Texture2D(Width,Height,TextureFormat.RGBA32,false);tex.name="Storyborn procedural pixel art";tex.filterMode=FilterMode.Point;tex.wrapMode=TextureWrapMode.Clamp;tex.SetPixels(pixels);tex.Apply();return tex; }
        }
    }
}
