#if UNITY_ANDROID || UNITY_IPHONE || UNITY_STANDALONE_OSX || UNITY_TVOS
// WARNING: Do not modify! Generated file.

namespace UnityEngine.Purchasing.Security {
    public class GooglePlayTangle
    {
        private static byte[] data = System.Convert.FromBase64String("P41oMirsGoKCzoO1M2YQZv2useZAsEU4EWMmsb9UvJ+FCpB8wed2hiu39m35ERLVHJeSyd78xOxJHVxvV5jksrdQRl/BakB9uZDjbIHsCQPoGBmedmtOP0dhOA1yTrE9BiylECwdAxLbzzpGY8Nxr6d+P76wpLc1/H9xfk78f3R8/H9/ftHisGPF1LDwsmILUYLONOUEaE3lDJogCjj7ZHFcJjTILg2GV8+Aqu4nFeY45M+JNLl/bUUHn7fAUWsgfP4iEs0wqthO/H9cTnN4d1T4NviJc39/f3t+fTkU3tKXJUM62+idqy7I+4OLsf23vsBG6cnha48RQAm7PXV0xl9t0bfHozmdTxEE5/sqQvvoiVZPlTPRSV7UufRXOSAWTXx9f35/");
        private static int[] order = new int[] { 2,9,4,11,6,13,9,8,8,10,10,13,13,13,14 };
        private static int key = 126;

        public static readonly bool IsPopulated = true;

        public static byte[] Data() {
        	if (IsPopulated == false)
        		return null;
            return Obfuscator.DeObfuscate(data, order, key);
        }
    }
}
#endif
