﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;

namespace CreamInstaller.Resources;

internal static class Resources
{
    internal static List<string> embeddedResources;
    internal static List<string> EmbeddedResources
    {
        get
        {
            if (embeddedResources is null)
            {
                string[] names = Assembly.GetExecutingAssembly().GetManifestResourceNames();
                embeddedResources = new();
                foreach (string resourceName in names)
                    if (resourceName.StartsWith("CreamInstaller.Resources."))
                        embeddedResources.Add(resourceName[25..]);
            }
            return embeddedResources;
        }
    }

    internal static void Write(this string resourceName, string filePath)
    {
        using Stream resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("CreamInstaller.Resources." + resourceName);
        using FileStream file = new(filePath, FileMode.Create, FileAccess.Write);
        resource.CopyTo(file);
    }

    internal static void Write(this byte[] resource, string filePath)
    {
        using FileStream fileStream = new(filePath, FileMode.Create, FileAccess.Write);
        fileStream.Write(resource);
    }

    internal static bool IsFilePathLocked(this string filePath)
    {
        try
        {
            File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None).Close();
        }
        catch (FileNotFoundException)
        {
            return false;
        }
        catch (IOException)
        {
            return true;
        }
        return false;
    }

    internal static void GetCreamApiComponents(
            this string directory,
            out string api32, out string api32_o,
            out string api64, out string api64_o,
            out string config)
    {
        api32 = directory + @"\steam_api.dll";
        api32_o = directory + @"\steam_api_o.dll";
        api64 = directory + @"\steam_api64.dll";
        api64_o = directory + @"\steam_api64_o.dll";
        config = directory + @"\cream_api.ini";
    }

    internal enum ResourceIdentifier
    {
        Koaloader,
        Steamworks32,
        Steamworks64,
        EpicOnlineServices32,
        EpicOnlineServices64,
        Uplay32,
        Uplay64,
        Upc32,
        Upc64,
    }

    internal static readonly Dictionary<ResourceIdentifier, IReadOnlyList<string>> ResourceMD5s = new()
    {
        {
            ResourceIdentifier.Koaloader,
            new List<string>()
            {
                "8A0958687B5ED7C34DAD037856DD1530", // Koaloader v2.0.0
                "8FECDEB40980F4E687C10E056232D96B", // Koaloader v2.0.0
                "92AD5E145A7CF9BA6F841BEB714B919B", // Koaloader v2.0.0
                "C7C8DA8FBAA24B1C622D75666DF911B1", // Koaloader v2.0.0
                "3DFC96D7240D950266F1BC3233712E7C", // Koaloader v2.0.0
                "4362BE74CE33EC9914D18D2EB331A925", // Koaloader v2.0.0
                "8DC726C52E6A0E5A50228D5182458B96", // Koaloader v2.0.0
                "43545091B955502D7D8ABEC636DFD544", // Koaloader v2.0.0
                "8A7CFFA985712715A066DFA9DD05C987", // Koaloader v2.0.0
                "E05F78063958369C91A1DD27CE4E773F", // Koaloader v2.0.0
                "8D320A22A37BB6D83A1B80B7D2D048ED", // Koaloader v2.0.0
                "2080910FC35F709896D22E9869B80037", // Koaloader v2.0.0
                "62979354919C9A2FDEF94AA2951EACF2", // Koaloader v2.0.0
                "6533B418AF8C48CDE461FF258EB7ACDC", // Koaloader v2.0.0
                "607C57906A0F0E584FA52D6BA38C4E30", // Koaloader v2.0.0
                "9D707A2A06E2E7A52649605C6999ED49", // Koaloader v2.0.0
                "FC16A38F46D2FEB17FBB9E83A9F89A83", // Koaloader v2.0.0
                "8DD69925C9E2AF938243F040B5393450", // Koaloader v2.0.0
                "DA6D66253164BE7DC82FCE6E53D91A4E", // Koaloader v2.0.0
                "3E46E48D86DE30A7ED54528B74416E8D", // Koaloader v2.0.0
                "D22A56619AFB38C8BB62647A9314318E", // Koaloader v2.0.0
                "AB1D17928A338317D5D8BAFE4BE704A7", // Koaloader v2.0.0
                "A1B1E61D7573E8BBB4EFEB38B971F9D0", // Koaloader v2.0.0
                "D64A719AE7C90C3890C826FE672CF7FC", // Koaloader v2.0.0
                "3FD5251708D8CC69C52A94F9DFFE5B82", // Koaloader v2.0.0
                "424EAD75A20B223FC79E752114682893", // Koaloader v2.0.0
                "AE5AFEEBD6B33597AAA76E7A10406C49", // Koaloader v2.0.0
                "B030110F89540F77AC016DD7E14DF6A0", // Koaloader v2.0.0
                "BEBF4B0EE207D1EE52AAF88ABE49345D", // Koaloader v2.0.0
                "A951E9B7EFF539D62451000D5A8BD0FA", // Koaloader v2.0.0
                "6C2166E9765D88D21C177C6ADBACA1A9", // Koaloader v2.0.0
                "193A2709F5DC979B6E95533651E1AB1B", // Koaloader v2.0.0
                "C9535AE9872D0F901D12A0943C7BFA9E", // Koaloader v2.0.0
                "F50EC987ECF69DDA070474F7E6633DA8", // Koaloader v2.0.0
                "081A7FEE3C7AE01727681B9A060F22B7", // Koaloader v2.0.0
                "1FBA57CF34D8D7F1CCB716CB36B48A68", // Koaloader v2.0.0
                "F7D7A38D85E72B3B4062BF8A6DCB88CF", // Koaloader v2.0.0
                "0CCA9698F3B3EE38A2F5312DB8959D16", // Koaloader v2.0.0
                "C86EB4610FF1FF005A81E7832B8980F4", // Koaloader v2.1.0
                "0E21677F4294F7EB70398294BAA984B1", // Koaloader v2.1.0
                "ABA42687E26A25D4842C089113D63106", // Koaloader v2.1.0
                "9E7F989BA0FB0EB903899E3CBA765EE6", // Koaloader v2.1.0
                "8DA8E426AD7AA35879FD0B454697DC70", // Koaloader v2.1.0
                "2A93D6DF7D140C195283FD5E6A59215D", // Koaloader v2.1.0
                "6416ADD92D5F9117EB36D3079DA651F4", // Koaloader v2.1.0
                "233BB87705ED93B8A1FC4FAB2BB4C703", // Koaloader v2.1.0
                "A3D13FC728F49D95B687FAE6C53AA7D4", // Koaloader v2.1.0
                "4A03856F2CB0B7DB21860B627AE957B5", // Koaloader v2.1.0
                "51894F12DC194305491868682873797B", // Koaloader v2.1.0
                "4F2B18B53BCA252ABEDAA323108D9C1A", // Koaloader v2.1.0
                "892F659ADFF6DF4E2ABFADDBD9399375", // Koaloader v2.1.0
                "7BEC8022A741F1B773C282F961748033", // Koaloader v2.1.0
                "CB5209E7CEBB6300BCAA3A59F9252F78", // Koaloader v2.1.0
                "64B4459B26112EC67210D02E17DD54CB", // Koaloader v2.1.0
                "109181378503A90DA6242B3F6F7E329F", // Koaloader v2.1.0
                "FDF0D8FC2FD2BB7C50F6D1C1731A43AF", // Koaloader v2.1.0
                "AB451B3D87F681F7224562B2B981B977", // Koaloader v2.1.0
                "26D996C70898B6FE4D6AD4AE4BF20259", // Koaloader v2.1.0
                "8797792DFB0E646145F7D0D3ABCFACC9", // Koaloader v2.1.0
                "50C40753084E292FDDD1D925EB347CB1", // Koaloader v2.1.0
                "15444227F746DB1A059348B8F4B4CCB8", // Koaloader v2.1.0
                "D5EADE4E60BF56B195C9B169221A10FF", // Koaloader v2.1.0
                "5C911D47785FC2120B8864388ADFB1DB", // Koaloader v2.1.0
                "45641372C0DC6C5DBB22809588FE38F6", // Koaloader v2.1.0
                "87D65C01F7D0BF5A2E39A4A7E3430309", // Koaloader v2.1.0
                "8F8FED4D8FB27045C5D4B2A9F2268705", // Koaloader v2.1.0
                "D3FC066A768CC196FA30E6E89597A3F1", // Koaloader v2.1.0
                "22A8CE664B9C490E049793345A82A2B9", // Koaloader v2.1.0
                "5EC45111CB3176A959E8A89CBA303B5B", // Koaloader v2.1.0
                "8693FDCDEC5B14F1E195CBB81D1E0607", // Koaloader v2.1.0
                "8E47DE51C27135E05D6F64B3E85968F9", // Koaloader v2.1.0
                "B1D439239FCB37D3863D805F45A56A22", // Koaloader v2.1.0
                "E113D3933EF0EDED1A5655EBC22FB7A4", // Koaloader v2.1.0
                "1D6BF8627DEFC15D9508A5DD2B59D4E5", // Koaloader v2.1.0
                "29AE5F96B6E3D33F1B67FD64C901918F", // Koaloader v2.1.0
                "7955E6067E5F4046DD8F52901ED96F8B", // Koaloader v2.1.0
                "B73402D229C5FD259D7D4D4082B300C1", // Koaloader v2.2.0
                "86DF72CD3A51E2BE2FFF3933F9C25D65", // Koaloader v2.2.0
                "3D6C089435F6CEC6C31B087EE8C5C1F3", // Koaloader v2.2.0
                "3CEE8A02D6603298166D471C6A508580", // Koaloader v2.2.0
                "F40BA1D04D881B717AA2B483E7026477", // Koaloader v2.2.0
                "02DF5703963745E67F26ABACA7930BF5", // Koaloader v2.2.0
                "E676242A25185CD07D6810ADB7102163", // Koaloader v2.2.0
                "5854D2080264317287FBB071B1235B48", // Koaloader v2.2.0
                "8DBCCD03569AA92E3E0FAA2C8B69D9AF", // Koaloader v2.2.0
                "5BA8528F504857CEE5D924ACBB40AEE0", // Koaloader v2.2.0
                "057E3F47E1E2AB4954533E48DEA936D5", // Koaloader v2.2.0
                "77CE58596FB07D240D0B4C8A3D0F1C16", // Koaloader v2.2.0
                "D65A3B7E13CBC6C42F19FEFDC192EE12", // Koaloader v2.2.0
                "3251A85FC95C8D5DA564845B4BA318DA", // Koaloader v2.2.0
                "4D6984BF1C2658C0E0FC109D856CC398", // Koaloader v2.2.0
                "167E73C133FFCCF547422733A9973FA6", // Koaloader v2.2.0
                "004E6824E492B10F4EDF507B279EFB06", // Koaloader v2.2.0
                "44D64F7AD3C9419F69CE81550C58DD8B", // Koaloader v2.2.0
                "E9FC8A37F685BFDA683314D4F1D2D9CA", // Koaloader v2.2.0
                "91BC9165D68284FC7E1A54D740E85E20", // Koaloader v2.2.0
                "68EE7096D6A7D38A8B4572A97D7F9F5F", // Koaloader v2.2.0
                "03DE57CC772F3A5D8E14803B3B52155C", // Koaloader v2.2.0
                "6389B8BDCE7AF237E91D5A02251F3997", // Koaloader v2.2.0
                "09F82329747FAB07E201493892534E58", // Koaloader v2.2.0
                "BDAF6E40573D92AA3EABBE5C06F50E86", // Koaloader v2.2.0
                "297A0A76114390FF8ADEE6D1787570C5", // Koaloader v2.2.0
                "4BE2E2D5561000F15C5869272FBA71D0", // Koaloader v2.2.0
                "60D2CE4FED68B420041CE3DE39A11FC0", // Koaloader v2.2.0
                "964FA5752932096AEFB5C0216E218DFA", // Koaloader v2.2.0
                "3EA0DCDA990275686E86313811C5F986", // Koaloader v2.2.0
                "12DBB504A7F12A4241C2AA18DAA4E61E", // Koaloader v2.2.0
                "B3E0506E739785B3E0581C7174FB46AF", // Koaloader v2.2.0
                "90555C58AEDC4933EFAA754D5027D71E", // Koaloader v2.2.0
                "2610596FB09B34330804DCDEB71EDBC5", // Koaloader v2.2.0
                "DEDF05749901B763D31742C0E2A591E4", // Koaloader v2.2.0
                "D9924348526BD1E49C9AA5657445EEE7", // Koaloader v2.2.0
                "7568B9964DF573AF47CEFB5F6D391D11", // Koaloader v2.2.0
                "4DC440F2A959160CCF5304AFC33C04C6", // Koaloader v2.2.0
                "C71C085632F7A1B0E4F9B71F94735D89", // Koaloader v2.2.0
                "6252B503416E4BBCF6F20566539B3A6C", // Koaloader v2.2.0
                "F5869981DBB2B8367E3F5946238CB085", // Koaloader v2.3.0
                "55EC5E259207105DBC7F66DEE160961D", // Koaloader v2.3.0
                "59CE9B64A1F9AF969EEE74325357FF6E", // Koaloader v2.3.0
                "BE55DE69FC12354278E5A25640B3C6A7", // Koaloader v2.3.0
                "E33ABDA11810EC85DB781891F1038325", // Koaloader v2.3.0
                "3CA9483190A42AF9B98BFC06C8A8921E", // Koaloader v2.3.0
                "9F3E427672B5FC88654ECA7A3E878C85", // Koaloader v2.3.0
                "A7F128F15B36049680719B0B23FAA5CC", // Koaloader v2.3.0
                "9300E79D8113F1B5B25792FA65DF3091", // Koaloader v2.3.0
                "4F711FE83CC721BB32A1445922CA6C66", // Koaloader v2.3.0
                "2D9F5B80DA0749C70F8ECAD4F9680EB1", // Koaloader v2.3.0
                "DD53BB4B94DDB098C0B6F67AEA238A73", // Koaloader v2.3.0
                "D21523497907024462D55E6866D675F0", // Koaloader v2.3.0
                "C86C6AADAAC4D4A02F4FDE4BFC952743", // Koaloader v2.3.0
                "D41F8F009690E759379AC3C770D1EBB1", // Koaloader v2.3.0
                "CB84703E7D89BDA83F15FDEEDB904070", // Koaloader v2.3.0
                "4F391ACDBD5A540EE792F7B72FAC85E4", // Koaloader v2.3.0
                "2A21EB5D0154C9670723D7AEA3AA6E0A", // Koaloader v2.3.0
                "59074F4F021E362A6B34CBEA5BAC4B5E", // Koaloader v2.3.0
                "2A68DF6BB7D1D6C64188F54A8B95E7B3", // Koaloader v2.3.0
                "5EFE90A8CA91125D9E9790294DAAE0AC", // Koaloader v2.3.0
                "356DAA845F1630F7383B932D73209EC2", // Koaloader v2.3.0
                "3F97154977E03B655407940850B8EBB5", // Koaloader v2.3.0
                "E0DE6BB745742665F008296C31D55092", // Koaloader v2.3.0
                "7E65A365253754E888A7C0613EE07C8F", // Koaloader v2.3.0
                "99E43FF15E7EF7862940362C64F4726F", // Koaloader v2.3.0
                "F1E2C14B334C1698F01A52A9985DC96D", // Koaloader v2.3.0
                "80B77C343564BE325622B3FAC54AFB81", // Koaloader v2.3.0
                "B4C1577E4CA7EB8FC5772B36CABD161F", // Koaloader v2.3.0
                "EDD0F897B06B12300B296667A1662BB0", // Koaloader v2.3.0
                "2F5FF638B9F36BF83DB4AE81FA273C70", // Koaloader v2.3.0
                "8189D2306AF77F1613BC2A9BDFF5030F", // Koaloader v2.3.0
                "753F5130CA873919B9617D2493F3F0DC", // Koaloader v2.3.0
                "9397A1F942B999E9CD2D383CA08117BE", // Koaloader v2.3.0
                "4BDE4C0BB7AA375C3A7FB4971659F3AF", // Koaloader v2.3.0
                "616A02C754B9EBC4AF78C5BD83153D92", // Koaloader v2.3.0
                "BBCB0FDCA5B82D867D687CF823D9265E", // Koaloader v2.3.0
                "0FA10F2F26468F75D6A053C2E0400B7B", // Koaloader v2.3.0
                "DCE0B4F44EDABB38FFF58E1CA0DC0A7A", // Koaloader v2.3.0
                "A5F71BAB5F0C898826F8AC8B558E771C", // Koaloader v2.3.0
                "58BA47FC35327387120476F8011B6DF1", // Koaloader v2.3.0
                "6CD7B5285B1673E4986345280B054AE4", // Koaloader v2.3.0
                "E67208EE62DB93881443E979AB3839B0", // Koaloader v2.3.1
                "88025169ED30BB032CD7C9361625511C", // Koaloader v2.3.1
                "7BA27686990BDB652E49DDA6B572B503", // Koaloader v2.3.1
                "32700AB3287B733AE566073B8D5812EF", // Koaloader v2.3.1
                "FBA4F8DE0461C8E55E974A50E9534D26", // Koaloader v2.3.1
                "EEB661285452F9575C99FC1A6B158C35", // Koaloader v2.3.1
                "3D5713F56CC6858A800C1F8BDA414C78", // Koaloader v2.3.1
                "3369CD82CA44DAC8B5C7ED6C3AA02F96", // Koaloader v2.3.1
                "0C479FF4BCE2F9C34BF848283843961C", // Koaloader v2.3.1
                "21AC5F783E6705966A49F1D086112BFE", // Koaloader v2.3.1
                "1243FB9AD8942FD0A9E97AF83B07E240", // Koaloader v2.3.1
                "883A613E430D96C87FCAA2CE276DA3E5", // Koaloader v2.3.1
                "406F174A2CE3EFDF7D9BE15492CEF128", // Koaloader v2.3.1
                "8FD65B5BF42F8B4F1A41D369907A8306", // Koaloader v2.3.1
                "BC1D6041DDDC5A75413D69EB093AB504", // Koaloader v2.3.1
                "DFF0092328520FA26D69E172C6347761", // Koaloader v2.3.1
                "16E86A47A475506C9ABE007B1C77620B", // Koaloader v2.3.1
                "97333C71D83B7BE06F4CF3F4AE0FC597", // Koaloader v2.3.1
                "4C98C464321F9D89609474F78D022F3F", // Koaloader v2.3.1
                "55A621ED89A30699EE96D623A90D3E64", // Koaloader v2.3.1
                "237A1D2AF7E7D4E8B5E5F0870EE0C4C9", // Koaloader v2.3.1
                "BFBD8C7ECC4A9FF9CBD0D295549757B9", // Koaloader v2.3.1
                "47FF6F0E0175BBECAB54E05AC2E34E3E", // Koaloader v2.3.1
                "DCC740ACAEB761F5D86F32867EE5F798", // Koaloader v2.3.1
                "56C31EBE85ACBD16BB71D1C7E0166AAC", // Koaloader v2.3.1
                "AD37B0F9BC149EA57A6FA413DD7EBB53", // Koaloader v2.3.1
                "ED358B63CD8921374F55E974E31B9F49", // Koaloader v2.3.1
                "F54124066AB67D4A3F0FE6F294B74ED4", // Koaloader v2.3.1
                "2CC13C25BC586F3E95834EFE2F76556F", // Koaloader v2.3.1
                "5F8021FDD3990600FE5D121B84A51E2A", // Koaloader v2.3.1
                "47B7F301BAC7D20E4E83EE50951996EF", // Koaloader v2.3.1
                "00ABEBB0948619B5590204E520ED6F32", // Koaloader v2.3.1
                "7CF7926AD8D00F8A971F9BD95FD37402", // Koaloader v2.3.1
                "D8DC9C0F6396679BE67651D876684002", // Koaloader v2.3.1
                "15D4CE713EA1DD76804D07719F9BD2DA", // Koaloader v2.3.1
                "F8DFB10A4B83EA226EDDC8182883A22E", // Koaloader v2.3.1
                "990FD060F819D921152065248A09FFFE", // Koaloader v2.3.1
                "9F6CC12477BDAF99BF25241F647B948D", // Koaloader v2.3.1
                "1CCC157A5B3D2436AAF530BB43218A04", // Koaloader v2.3.1
                "8F0F5739AA690FF2413533906B238341", // Koaloader v2.3.1
                "C4C84E1D298718F41B50523CAFD9B2DD", // Koaloader v2.3.1
                "86EDC4E7F8E8CC00BA4C6AF0BF3B99D6", // Koaloader v2.3.1
                "D179A8E9E2703AEA5E7BA79301A0BC0B", // Koaloader v2.4.0
                "C7AA775BE68D2BD3CE9D688F6FF8D99B", // Koaloader v2.4.0
                "5B443AC74DCDBC5E0D30338E071C69B2", // Koaloader v2.4.0
                "A1033841B169A69DBD19571D57FF6FC8", // Koaloader v2.4.0
                "AA026E5929A3F179C501AD6A38449641", // Koaloader v2.4.0
                "B0CE4971F594D00FB766626E36FCD1CA", // Koaloader v2.4.0
                "3EA85CDD687D0D2320B9E10F5D594132", // Koaloader v2.4.0
                "ED157D5C6797FB938D4242124CEBBAAA", // Koaloader v2.4.0
                "B56883CAF9615EB98C63C13C3DC77051", // Koaloader v2.4.0
                "E66DA96DDF74665E065263A00E95A583", // Koaloader v2.4.0
                "DEF38C4AC4FD4E87D92F94E7A4FBB76F", // Koaloader v2.4.0
                "A749067247FC271CFAF9DE3F48A4F05E", // Koaloader v2.4.0
                "58F4CAABAE1A18C1C078CC5BBA8BA94F", // Koaloader v2.4.0
                "DD7DD366CEA4E5D082B310998E59422A", // Koaloader v2.4.0
                "E70717F2B978087A36464BBFC24F2C39", // Koaloader v2.4.0
                "DD458B242E7C682BC71420BABF8C4781", // Koaloader v2.4.0
                "52FDC73FF606321E0EDD6E206D251839", // Koaloader v2.4.0
                "5F0886B24E1542C582ED3CA7FA5DD8ED", // Koaloader v2.4.0
                "A150752FCDEAF079534CE1D12D0C5839", // Koaloader v2.4.0
                "BBDC38C52C343B119FEEEAB09A0B7BBF", // Koaloader v2.4.0
                "3FDB75A22AF7FA4DA049544CDE90F2FC", // Koaloader v2.4.0
                "E767CA1BAFE5A5FDECE824985C37B37A", // Koaloader v2.4.0
                "4A77F267BBCADD26DB8D11412B5CE481", // Koaloader v2.4.0
                "D5FE770ACA2AB85480F45C9A567E6316", // Koaloader v2.4.0
                "2CC10D94D726EC58DB9791313AF2348D", // Koaloader v2.4.0
                "AA40C72E293E70D9C45DDE4642FBD410", // Koaloader v2.4.0
                "9895A1E8E249AAAF79526BE87A7FDC4E", // Koaloader v2.4.0
                "AA0EA940723098BE42263DD462FC6ED5", // Koaloader v2.4.0
                "C62E29F4BA74BF14F8899FDDDA8453D9", // Koaloader v2.4.0
                "F41DEF0750781450D1DEAB534B005567", // Koaloader v2.4.0
                "6AB9CE2D069779AE237E3201742CF0B3", // Koaloader v2.4.0
                "4BDFF1EAE37128F28B0756D924785EBC", // Koaloader v2.4.0
                "AF6C6C5F00F3376730BDCD2CE3376655", // Koaloader v2.4.0
                "775D8DD35CA7C16645EDE3B7CD1BF895", // Koaloader v2.4.0
                "0338F46A9A426AA1EA6B52B92221F598", // Koaloader v2.4.0
                "39F7ABE418A2B34DAD413C6532BE5785", // Koaloader v2.4.0
                "EBD337EBC1ABC22031FD88AA3BAA1F31", // Koaloader v2.4.0
                "7D3DBD6445CDE872A37331D92718E586", // Koaloader v2.4.0
                "0BEADDA6A89B7F930D3E3AD1438F9E5C", // Koaloader v2.4.0
                "C130AE45661BDE4E9177A487C89F82BF", // Koaloader v2.4.0
                "76CAB00C7DD33FC19F7CDD1849FF9CA2", // Koaloader v2.4.0
                "DA4D6A7C0872757A74DDAE05A2C1D160", // Koaloader v2.4.0
                "1F46DE8747C0A157841AFFE6185CE4C9", // Koaloader v2.4.0
                "BE16B588D018D8EFF1F3B6A600F26BED"  // Koaloader v2.4.0
            }
        },
        {
            ResourceIdentifier.EpicOnlineServices32,
            new List<string>()
            {
                "069A57B1834A960193D2AD6B96926D70", // ScreamAPI v3.0.0
                "E2FB3A4A9583FDC215832E5F935E4440"  // ScreamAPI v3.0.1
            }
        },
        {
            ResourceIdentifier.EpicOnlineServices64,
            new List<string>()
            {
                "0D62E57139F1A64F807A9934946A9474", // ScreamAPI v3.0.0
                "3875C7B735EE80C23239CC4749FDCBE6"  // ScreamAPI v3.0.1
            }
        },
        {
            ResourceIdentifier.Steamworks32,
            new List<string>()
            {
                "02594110FE56B2945955D46670B9A094", // CreamAPI v4.5.0.0 Hotfix
                "B2434578957CBE38BDCE0A671C1262FC", // SmokeAPI v1.0.0
                "973AB1632B747D4BF3B2666F32E34327", // SmokeAPI v1.0.1
                "C7E41F569FC6A347D67D2BFB2BD10F25", // SmokeAPI v1.0.2
                "F9E7D5B248B86D1C2F2F2905A9F37755"  // SmokeAPI v1.0.3
            }
        },
        {
            ResourceIdentifier.Steamworks64,
            new List<string>()
            {
                "30091B91923D9583A54A93ED1145554B", // CreamAPI v4.5.0.0 Hotfix
                "08713035CAD6F52548FF324D0487B88D", // SmokeAPI v1.0.0
                "D077737B9979D32458AC938A2978FA3C", // SmokeAPI v1.0.1
                "49122A2E2E51CBB0AE5E1D59B280E4CD", // SmokeAPI v1.0.2
                "13F3E9476116F7670E21365A400357AC"  // SmokeAPI v1.0.3
            }
        },
        {
            ResourceIdentifier.Uplay32,
            new List<string>()
            {
                "1977967B2549A38EC2DB39D4C8ED499B" // Uplay R1 Unlocker v2.0.0
            }
        },
        {
            ResourceIdentifier.Uplay64,
            new List<string>()
            {
                "333FEDD9DC2B299419B37ED1624FF8DB" // Uplay R1 Unlocker v2.0.0
            }
        },
        {
            ResourceIdentifier.Upc32,
            new List<string>()
            {
                "C14368BC4EE19FDE8DBAC07E31C67AE4", // Uplay R2 Unlocker v3.0.0
                "DED3A3EA1876E3110D7D87B9A22946B0"  // Uplay R2 Unlocker v3.0.1
            }
        },
        {
            ResourceIdentifier.Upc64,
            new List<string>()
            {
                "7D9A4C12972BAABCB6C181920CC0F19B", // Uplay R2 Unlocker v3.0.0
                "D7FDBFE0FC8D7600FEB8EC0A97713184"  // Uplay R2 Unlocker v3.0.1
            }
        }
    };

    internal static string ComputeMD5(this string filePath)
    {
        if (!File.Exists(filePath)) return null;
#pragma warning disable CA5351 // Do Not Use Broken Cryptographic Algorithms
        using MD5 md5 = MD5.Create();
#pragma warning restore CA5351 // Do Not Use Broken Cryptographic Algorithms
        using FileStream stream = File.OpenRead(filePath);
        byte[] hash = md5.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
    }

    internal static bool IsResourceFile(this string filePath, ResourceIdentifier identifier) => filePath.ComputeMD5() is string hash && ResourceMD5s[identifier].Contains(hash);

    internal static bool IsResourceFile(this string filePath) => filePath.ComputeMD5() is string hash && ResourceMD5s.Values.Any(hashes => hashes.Contains(hash));
}