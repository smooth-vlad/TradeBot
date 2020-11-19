using OxyPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeBot
{
    public static class ShapesPaths
    {
        public static readonly ScreenPoint[] arrowUp = new ScreenPoint[]
        {
            new ScreenPoint( 0.00029779876708979636 ,  -0.24970157623291012 ),
            new ScreenPoint( 0.04427529823780063 ,  -0.20572470040321356 ),
            new ScreenPoint( 0.08817485317476104 ,  -0.16182470247641206 ),
            new ScreenPoint( 0.13269182288646708 ,  -0.11730728354454045 ),
            new ScreenPoint( 0.1767044906068127 ,  -0.0732941716929339 ),
            new ScreenPoint( 0.22098802762925618 ,  -0.02901018780589104 ),
            new ScreenPoint( 0.19512860770225515 ,  0.014773759751319893 ),
            new ScreenPoint( 0.1506962484959512 ,  -0.010914035148397061 ),
            new ScreenPoint( 0.10669233240783216 ,  -0.05491830723941321 ),
            new ScreenPoint( 0.06251614664137362 ,  -0.0990948504024744 ),
            new ScreenPoint( 0.031249375000000024 ,  -0.11160618734739725 ),
            new ScreenPoint( 0.031249375000000024 ,  -0.049040613944157985 ),
            new ScreenPoint( 0.031249375000000024 ,  0.013599984368681928 ),
            new ScreenPoint( 0.031249375000000024 ,  0.07544406250000002 ),
            new ScreenPoint( 0.031249375000000024 ,  0.13844331692704004 ),
            new ScreenPoint( 0.031249375000000024 ,  0.20090995705756365 ),
            new ScreenPoint( 0.031249375000000024 ,  0.26356355224609374 ),
            new ScreenPoint( -0.012836604461669898 ,  0.28125 ),
            new ScreenPoint( -0.03125062499999992 ,  0.23718888163566598 ),
            new ScreenPoint( -0.031250624999999976 ,  0.17415053282165893 ),
            new ScreenPoint( -0.031250624999999976 ,  0.11141119590029114 ),
            new ScreenPoint( -0.031250624999999976 ,  0.048919391352683306 ),
            new ScreenPoint( -0.031250624999999976 ,  -0.013587971776723884 ),
            new ScreenPoint( -0.031250624999999976 ,  -0.0757956788293086 ),
            new ScreenPoint( -0.036395150690078704 ,  -0.1253242035150528 ),
            new ScreenPoint( -0.08102486584410068 ,  -0.08069430796168747 ),
            new ScreenPoint( -0.12564851677492256 ,  -0.03607047665603458 ),
            new ScreenPoint( -0.16932469273567202 ,  0.007605875849723831 ),
            new ScreenPoint( -0.2136098959416151 ,  -0.003584503668546679 ),
            new ScreenPoint( -0.20232136824768038 ,  -0.04778550675231963 ),
            new ScreenPoint( -0.15854969411700964 ,  -0.09155718088299031 ),
            new ScreenPoint( -0.1137122979274392 ,  -0.1363945770725608 ),
            new ScreenPoint( -0.06968928178850564 ,  -0.18041759321149442 ),
            new ScreenPoint( -0.02554794310539965 ,  -0.2245589318946004 ),
        };

        public static readonly ScreenPoint[] plus = new ScreenPoint[]
        {
            new ScreenPoint( -0.1875 ,  0.0625 ),
            new ScreenPoint( -0.1875 ,  -0.0625 ),
            new ScreenPoint( -0.0625 ,  -0.0625 ),
            new ScreenPoint( -0.0625 ,  -0.1875 ),
            new ScreenPoint( 0.0625 ,  -0.1875 ),
            new ScreenPoint( 0.0625 ,  -0.0625 ),
            new ScreenPoint( 0.1875 ,  -0.0625 ),
            new ScreenPoint( 0.1875 ,  0.0625 ),
            new ScreenPoint( 0.0625 ,  0.0625 ),
            new ScreenPoint( 0.0625 ,  0.1875 ),
            new ScreenPoint( -0.0625 ,  0.1875 ),
            new ScreenPoint( -0.0625 ,  0.0625 ),
            new ScreenPoint( -0.1875 ,  0.0625 ),
        };

        public static readonly ScreenPoint[] minus = new ScreenPoint[]
        {
            new ScreenPoint( -0.1875 ,  0.0625 ),
            new ScreenPoint( -0.1875 ,  -0.0625 ),
            new ScreenPoint( 0.1875 ,  -0.0625 ),
            new ScreenPoint( 0.1875 ,  0.0625 ),
            new ScreenPoint( -0.1875 ,  0.0625 ),
        };

        public static readonly ScreenPoint[] arrowDown = new ScreenPoint[]
        {
            new ScreenPoint( -0.00041218078613286524 ,  0.2808984161376953 ),
            new ScreenPoint( -0.044389674596786555 ,  0.23692157540321346 ),
            new ScreenPoint( -0.08828967252358794 ,  0.19302157747641202 ),
            new ScreenPoint( -0.1328070914554596 ,  0.14850415854454035 ),
            new ScreenPoint( -0.17682020330706616 ,  0.10449104669293385 ),
            new ScreenPoint( -0.22110418719410896 ,  0.060207062805891054 ),
            new ScreenPoint( -0.19524436524868016 ,  0.01642311524868012 ),
            new ScreenPoint( -0.1508117316737771 ,  0.04211034285970028 ),
            new ScreenPoint( -0.10680763758420947 ,  0.0861139029449225 ),
            new ScreenPoint( -0.0626312731194496 ,  0.1302897313147784 ),
            new ScreenPoint( -0.031364374999999944 ,  0.14280067626349635 ),
            new ScreenPoint( -0.031364374999999944 ,  0.0802354828637093 ),
            new ScreenPoint( -0.031364375 ,  0.017595265009999195 ),
            new ScreenPoint( -0.031364375 ,  -0.044248437500000015 ),
            new ScreenPoint( -0.031364375 ,  -0.10724730928954673 ),
            new ScreenPoint( -0.031364375 ,  -0.1697135700175073 ),
            new ScreenPoint( -0.031364375 ,  -0.23236678466796873 ),
            new ScreenPoint( 0.012721604461669922 ,  -0.250053125 ),
            new ScreenPoint( 0.03113562499999989 ,  -0.20599234106540681 ),
            new ScreenPoint( 0.031135625 ,  -0.1429544707208406 ),
            new ScreenPoint( 0.031135625 ,  -0.0802156099993735 ),
            new ScreenPoint( 0.03113562500000011 ,  -0.01772427977286284 ),
            new ScreenPoint( 0.031135625 ,  0.044782608917355526 ),
            new ScreenPoint( 0.031135625 ,  0.10698984380518084 ),
            new ScreenPoint( 0.037008128653168604 ,  0.15579004382193085 ),
            new ScreenPoint( 0.08090966464988891 ,  0.11188886273853493 ),
            new ScreenPoint( 0.12553313520587972 ,  0.06726575293220582 ),
            new ScreenPoint( 0.16920913462162013 ,  0.02359010660648353 ),
            new ScreenPoint( 0.21349387866854663 ,  0.03478114594161519 ),
            new ScreenPoint( 0.20220534441648974 ,  0.07898238175231964 ),
            new ScreenPoint( 0.15843402710072696 ,  0.12275405588299038 ),
            new ScreenPoint( 0.11359699641354393 ,  0.16759145207256076 ),
            new ScreenPoint( 0.0695743391383905 ,  0.21161446821149443 ),
            new ScreenPoint( 0.025433360283598327 ,  0.2557558068946004 ),
        };
    }
}
