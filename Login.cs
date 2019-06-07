using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Diagnostics;
using System.Management;
using Common.Security;
using System.Net.NetworkInformation;
using System.Configuration;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Microsoft.Win32;
using System.Reflection;

namespace TNT
{
    public partial class Login : Form
    {

        /// ////////////////////////////login: license checking///////////////////
        /// </summary>
        /// 
        //Constant definitions from setupapi.h, which we aren't allowed to include directly since this is C#
        internal const uint DIGCF_PRESENT = 0x02;
        internal const uint DIGCF_DEVICEINTERFACE = 0x10;
        //Constants for CreateFile() and other file I/O functions
        internal const short FILE_ATTRIBUTE_NORMAL = 0x80;
        internal const short INVALID_HANDLE_VALUE = -1;
        internal const uint GENERIC_READ = 0x80000000;
        internal const uint GENERIC_WRITE = 0x40000000;
        internal const uint CREATE_NEW = 1;
        internal const uint CREATE_ALWAYS = 2;
        internal const uint OPEN_EXISTING = 3;
        internal const uint FILE_SHARE_READ = 0x00000001;
        internal const uint FILE_SHARE_WRITE = 0x00000002;
        //Constant definitions for certain WM_DEVICECHANGE messages
        internal const uint WM_DEVICECHANGE = 0x0219;
        internal const uint DBT_DEVICEARRIVAL = 0x8000;
        internal const uint DBT_DEVICEREMOVEPENDING = 0x8003;
        internal const uint DBT_DEVICEREMOVECOMPLETE = 0x8004;
        internal const uint DBT_CONFIGCHANGED = 0x0018;
        //Other constant definitions
        internal const uint DBT_DEVTYP_DEVICEINTERFACE = 0x05;
        internal const uint DEVICE_NOTIFY_WINDOW_HANDLE = 0x00;
        internal const uint ERROR_SUCCESS = 0x00;
        internal const uint ERROR_NO_MORE_ITEMS = 0x00000103;
        internal const uint SPDRP_HARDWAREID = 0x00000001;

        //Various structure definitions for structures that this code will be using
        internal struct SP_DEVICE_INTERFACE_DATA
        {
            internal uint cbSize;               //DWORD
            internal Guid InterfaceClassGuid;   //GUID
            internal uint Flags;                //DWORD
            internal uint Reserved;             //ULONG_PTR MSDN says ULONG_PTR is "typedef unsigned __int3264 ULONG_PTR;"  
        }

        internal struct SP_DEVICE_INTERFACE_DETAIL_DATA
        {
            internal uint cbSize;               //DWORD
            internal char[] DevicePath;         //TCHAR array of any size
        }

        internal struct SP_DEVINFO_DATA
        {
            internal uint cbSize;       //DWORD
            internal Guid ClassGuid;    //GUID
            internal uint DevInst;      //DWORD
            internal uint Reserved;     //ULONG_PTR  MSDN says ULONG_PTR is "typedef unsigned __int3264 ULONG_PTR;"  
        }

        internal struct DEV_BROADCAST_DEVICEINTERFACE
        {
            internal uint dbcc_size;            //DWORD
            internal uint dbcc_devicetype;      //DWORD
            internal uint dbcc_reserved;        //DWORD
            internal Guid dbcc_classguid;       //GUID
            internal char[] dbcc_name;          //TCHAR array
        }

        //DLL Imports.  Need these to access various C style unmanaged functions contained in their respective DLL files.
        //--------------------------------------------------------------------------------------------------------------
        //Returns a HDEVINFO type for a device information set.  We will need the 
        //HDEVINFO as in input parameter for calling many of the other SetupDixxx() functions.
        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern IntPtr SetupDiGetClassDevs(
            ref Guid ClassGuid,     //LPGUID    Input: Need to supply the class GUID. 
            IntPtr Enumerator,      //PCTSTR    Input: Use NULL here, not important for our purposes
            IntPtr hwndParent,      //HWND      Input: Use NULL here, not important for our purposes
            uint Flags);            //DWORD     Input: Flags describing what kind of filtering to use.

        //Gives us "PSP_DEVICE_INTERFACE_DATA" which contains the Interface specific GUID (different
        //from class GUID).  We need the interface GUID to get the device path.
        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool SetupDiEnumDeviceInterfaces(
            IntPtr DeviceInfoSet,           //Input: Give it the HDEVINFO we got from SetupDiGetClassDevs()
            IntPtr DeviceInfoData,          //Input (optional)
            ref Guid InterfaceClassGuid,    //Input 
            uint MemberIndex,               //Input: "Index" of the device you are interested in getting the path for.
            ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData);    //Output: This function fills in an "SP_DEVICE_INTERFACE_DATA" structure.

        //SetupDiDestroyDeviceInfoList() frees up memory by destroying a DeviceInfoList
        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool SetupDiDestroyDeviceInfoList(
            IntPtr DeviceInfoSet);          //Input: Give it a handle to a device info list to deallocate from RAM.

        //SetupDiEnumDeviceInfo() fills in an "SP_DEVINFO_DATA" structure, which we need for SetupDiGetDeviceRegistryProperty()
        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool SetupDiEnumDeviceInfo(
            IntPtr DeviceInfoSet,
            uint MemberIndex,
            ref SP_DEVINFO_DATA DeviceInterfaceData);

        //SetupDiGetDeviceRegistryProperty() gives us the hardware ID, which we use to check to see if it has matching VID/PID
        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool SetupDiGetDeviceRegistryProperty(
            IntPtr DeviceInfoSet,
            ref SP_DEVINFO_DATA DeviceInfoData,
            uint Property,
            ref uint PropertyRegDataType,
            IntPtr PropertyBuffer,
            uint PropertyBufferSize,
            ref uint RequiredSize);

        //SetupDiGetDeviceInterfaceDetail() gives us a device path, which is needed before CreateFile() can be used.
        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool SetupDiGetDeviceInterfaceDetail(
            IntPtr DeviceInfoSet,                   //Input: Wants HDEVINFO which can be obtained from SetupDiGetClassDevs()
            ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData,                    //Input: Pointer to an structure which defines the device interface.  
            IntPtr DeviceInterfaceDetailData,      //Output: Pointer to a SP_DEVICE_INTERFACE_DETAIL_DATA structure, which will receive the device path.
            uint DeviceInterfaceDetailDataSize,     //Input: Number of bytes to retrieve.
            ref uint RequiredSize,                  //Output (optional): The number of bytes needed to hold the entire struct 
            IntPtr DeviceInfoData);                 //Output (optional): Pointer to a SP_DEVINFO_DATA structure

        //Overload for SetupDiGetDeviceInterfaceDetail().  Need this one since we can't pass NULL pointers directly in C#.
        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool SetupDiGetDeviceInterfaceDetail(
            IntPtr DeviceInfoSet,                   //Input: Wants HDEVINFO which can be obtained from SetupDiGetClassDevs()
            ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData,               //Input: Pointer to an structure which defines the device interface.  
            IntPtr DeviceInterfaceDetailData,       //Output: Pointer to a SP_DEVICE_INTERFACE_DETAIL_DATA structure, which will contain the device path.
            uint DeviceInterfaceDetailDataSize,     //Input: Number of bytes to retrieve.
            IntPtr RequiredSize,                    //Output (optional): Pointer to a DWORD to tell you the number of bytes needed to hold the entire struct 
            IntPtr DeviceInfoData);                 //Output (optional): Pointer to a SP_DEVINFO_DATA structure

        //Need this function for receiving all of the WM_DEVICECHANGE messages.  See MSDN documentation for
        //description of what this function does/how to use it. Note: name is remapped "RegisterDeviceNotificationUM" to
        //avoid possible build error conflicts.
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern IntPtr RegisterDeviceNotification(
            IntPtr hRecipient,
            IntPtr NotificationFilter,
            uint Flags);

        //Takes in a device path and opens a handle to the device.
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern SafeFileHandle CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        //Uses a handle (created with CreateFile()), and lets us write USB data to the device.
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern bool WriteFile(
            SafeFileHandle hFile,
            byte[] lpBuffer,
            uint nNumberOfBytesToWrite,
            ref uint lpNumberOfBytesWritten,
            IntPtr lpOverlapped);

        //Uses a handle (created with CreateFile()), and lets us read USB data from the device.
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern bool ReadFile(
            SafeFileHandle hFile,
            IntPtr lpBuffer,
            uint nNumberOfBytesToRead,
            ref uint lpNumberOfBytesRead,
            IntPtr lpOverlapped);



        //--------------- Global Varibles Section ------------------
        //USB related variables that need to have wide scope.
        bool AttachedState = false;						//Need to keep track of the USB device attachment status for proper plug and play operation.
        bool AttachedButBroken = false;
        bool bRegisteredLic = false;
        bool bLicExpire = false;
        SafeFileHandle WriteHandleToUSBDevice = null;
        SafeFileHandle ReadHandleToUSBDevice = null;
        String DevicePath = null;   //Need the find the proper device path before you can open file handles.


        //Variables used by the application/form updates.
        bool PushbuttonPressed = false;		//Updated by ReadWriteThread, read by FormUpdateTimer tick handler (needs to be atomic)
        bool ToggleLEDsPending = false;		//Updated by ToggleLED(s) button click event handler, used by ReadWriteThread (needs to be atomic)
        uint ADCValue = 0;			//Updated by ReadWriteThread, read by FormUpdateTimer tick handler (needs to be atomic)

        //Globally Unique Identifier (GUID) for HID class devices.  Windows uses GUIDs to identify things.
        Guid InterfaceClassGuid = new Guid(0x4d1e55b2, 0xf16f, 0x11cf, 0x88, 0xcb, 0x00, 0x11, 0x11, 0x00, 0x00, 0x30);
        //--------------- End of Global Varibles ------------------

        //FUNCTION:	CheckIfPresentAndGetUSBDevicePath()
        //PURPOSE:	Check if a USB device is currently plugged in with a matching VID and PID
        //INPUT:	Uses globally declared String DevicePath, globally declared GUID, and the MY_DEVICE_ID constant.
        //OUTPUT:	Returns BOOL.  TRUE when device with matching VID/PID found.  FALSE if device with VID/PID could not be found.
        //			When returns TRUE, the globally accessable "DetailedInterfaceDataStructure" will contain the device path
        //			to the USB device with the matching VID/PID.

        bool CheckIfPresentAndGetUSBDevicePath()
        {
            /* 
            Before we can "connect" our application to our USB embedded device, we must first find the device.
            A USB bus can have many devices simultaneously connected, so somehow we have to find our device only.
            This is done with the Vendor ID (VID) and Product ID (PID).  Each USB product line should have
            a unique combination of VID and PID.  

            Microsoft has created a number of functions which are useful for finding plug and play devices.  Documentation
            for each function used can be found in the MSDN library.  We will be using the following functions (unmanaged C functions):

            SetupDiGetClassDevs()					//provided by setupapi.dll, which comes with Windows
            SetupDiEnumDeviceInterfaces()			//provided by setupapi.dll, which comes with Windows
            GetLastError()							//provided by kernel32.dll, which comes with Windows
            SetupDiDestroyDeviceInfoList()			//provided by setupapi.dll, which comes with Windows
            SetupDiGetDeviceInterfaceDetail()		//provided by setupapi.dll, which comes with Windows
            SetupDiGetDeviceRegistryProperty()		//provided by setupapi.dll, which comes with Windows
            CreateFile()							//provided by kernel32.dll, which comes with Windows

            In order to call these unmanaged functions, the Marshal class is very useful.
             
            We will also be using the following unusual data types and structures.  Documentation can also be found in
            the MSDN library:

            PSP_DEVICE_INTERFACE_DATA
            PSP_DEVICE_INTERFACE_DETAIL_DATA
            SP_DEVINFO_DATA
            HDEVINFO
            HANDLE
            GUID

            The ultimate objective of the following code is to get the device path, which will be used elsewhere for getting
            read and write handles to the USB device.  Once the read/write handles are opened, only then can this
            PC application begin reading/writing to the USB device using the WriteFile() and ReadFile() functions.

            Getting the device path is a multi-step round about process, which requires calling several of the
            SetupDixxx() functions provided by setupapi.dll.
            */

            try
            {
                IntPtr DeviceInfoTable = IntPtr.Zero;
                SP_DEVICE_INTERFACE_DATA InterfaceDataStructure = new SP_DEVICE_INTERFACE_DATA();
                SP_DEVICE_INTERFACE_DETAIL_DATA DetailedInterfaceDataStructure = new SP_DEVICE_INTERFACE_DETAIL_DATA();
                SP_DEVINFO_DATA DevInfoData = new SP_DEVINFO_DATA();

                uint InterfaceIndex = 0;
                uint dwRegType = 0;
                uint dwRegSize = 0;
                uint dwRegSize2 = 0;
                uint StructureSize = 0;
                IntPtr PropertyValueBuffer = IntPtr.Zero;
                bool MatchFound = false;
                uint ErrorStatus;
                uint LoopCounter = 0;

                //Use the formatting: "Vid_xxxx&Pid_xxxx" where xxxx is a 16-bit hexadecimal number.
                //Make sure the value appearing in the parathesis matches the USB device descriptor
                //of the device that this aplication is intending to find.
                String DeviceIDToFind = "Vid_04d8&Pid_003f";

                //First populate a list of plugged in devices (by specifying "DIGCF_PRESENT"), which are of the specified class GUID. 
                DeviceInfoTable = SetupDiGetClassDevs(ref InterfaceClassGuid, IntPtr.Zero, IntPtr.Zero, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);

                if (DeviceInfoTable != IntPtr.Zero)
                {
                    //Now look through the list we just populated.  We are trying to see if any of them match our device. 
                    while (true)
                    {
                        InterfaceDataStructure.cbSize = (uint)Marshal.SizeOf(InterfaceDataStructure);
                        if (SetupDiEnumDeviceInterfaces(DeviceInfoTable, IntPtr.Zero, ref InterfaceClassGuid, InterfaceIndex, ref InterfaceDataStructure))
                        {
                            ErrorStatus = (uint)Marshal.GetLastWin32Error();
                            if (ErrorStatus == ERROR_NO_MORE_ITEMS)	//Did we reach the end of the list of matching devices in the DeviceInfoTable?
                            {	//Cound not find the device.  Must not have been attached.
                                SetupDiDestroyDeviceInfoList(DeviceInfoTable);	//Clean up the old structure we no longer need.
                                return false;
                            }
                        }
                        else	//Else some other kind of unknown error ocurred...
                        {
                            ErrorStatus = (uint)Marshal.GetLastWin32Error();
                            SetupDiDestroyDeviceInfoList(DeviceInfoTable);	//Clean up the old structure we no longer need.
                            return false;
                        }

                        //Now retrieve the hardware ID from the registry.  The hardware ID contains the VID and PID, which we will then 
                        //check to see if it is the correct device or not.

                        //Initialize an appropriate SP_DEVINFO_DATA structure.  We need this structure for SetupDiGetDeviceRegistryProperty().
                        DevInfoData.cbSize = (uint)Marshal.SizeOf(DevInfoData);
                        SetupDiEnumDeviceInfo(DeviceInfoTable, InterfaceIndex, ref DevInfoData);

                        //First query for the size of the hardware ID, so we can know how big a buffer to allocate for the data.
                        SetupDiGetDeviceRegistryProperty(DeviceInfoTable, ref DevInfoData, SPDRP_HARDWAREID, ref dwRegType, IntPtr.Zero, 0, ref dwRegSize);

                        //Allocate a buffer for the hardware ID.
                        //Should normally work, but could throw exception "OutOfMemoryException" if not enough resources available.
                        PropertyValueBuffer = Marshal.AllocHGlobal((int)dwRegSize);

                        //Retrieve the hardware IDs for the current device we are looking at.  PropertyValueBuffer gets filled with a 
                        //REG_MULTI_SZ (array of null terminated strings).  To find a device, we only care about the very first string in the
                        //buffer, which will be the "device ID".  The device ID is a string which contains the VID and PID, in the example 
                        //format "Vid_04d8&Pid_003f".
                        SetupDiGetDeviceRegistryProperty(DeviceInfoTable, ref DevInfoData, SPDRP_HARDWAREID, ref dwRegType, PropertyValueBuffer, dwRegSize, ref dwRegSize2);

                        //Now check if the first string in the hardware ID matches the device ID of the USB device we are trying to find.
                        String DeviceIDFromRegistry = Marshal.PtrToStringUni(PropertyValueBuffer); //Make a new string, fill it with the contents from the PropertyValueBuffer

                        Marshal.FreeHGlobal(PropertyValueBuffer);		//No longer need the PropertyValueBuffer, free the memory to prevent potential memory leaks

                        //Convert both strings to lower case.  This makes the code more robust/portable accross OS Versions
                        DeviceIDFromRegistry = DeviceIDFromRegistry.ToLowerInvariant();
                        DeviceIDToFind = DeviceIDToFind.ToLowerInvariant();
                        //Now check if the hardware ID we are looking at contains the correct VID/PID
                        MatchFound = DeviceIDFromRegistry.Contains(DeviceIDToFind);
                        Console.WriteLine("\n LICENSE device id: " + DeviceIDToFind + " device from registry: " + DeviceIDFromRegistry);
                        Console.WriteLine("\n LICENSE device id: match found " + MatchFound.ToString());
                        if (MatchFound == true)
                        {
                            //Device must have been found.  In order to open I/O file handle(s), we will need the actual device path first.
                            //We can get the path by calling SetupDiGetDeviceInterfaceDetail(), however, we have to call this function twice:  The first
                            //time to get the size of the required structure/buffer to hold the detailed interface data, then a second time to actually 
                            //get the structure (after we have allocated enough memory for the structure.)
                            DetailedInterfaceDataStructure.cbSize = (uint)Marshal.SizeOf(DetailedInterfaceDataStructure);
                            //First call populates "StructureSize" with the correct value
                            SetupDiGetDeviceInterfaceDetail(DeviceInfoTable, ref InterfaceDataStructure, IntPtr.Zero, 0, ref StructureSize, IntPtr.Zero);
                            //Need to call SetupDiGetDeviceInterfaceDetail() again, this time specifying a pointer to a SP_DEVICE_INTERFACE_DETAIL_DATA buffer with the correct size of RAM allocated.
                            //First need to allocate the unmanaged buffer and get a pointer to it.
                            IntPtr pUnmanagedDetailedInterfaceDataStructure = IntPtr.Zero;  //Declare a pointer.
                            pUnmanagedDetailedInterfaceDataStructure = Marshal.AllocHGlobal((int)StructureSize);    //Reserve some unmanaged memory for the structure.
                            DetailedInterfaceDataStructure.cbSize = 6; //Initialize the cbSize parameter (4 bytes for DWORD + 2 bytes for unicode null terminator)
                            Marshal.StructureToPtr(DetailedInterfaceDataStructure, pUnmanagedDetailedInterfaceDataStructure, false); //Copy managed structure contents into the unmanaged memory buffer.

                            //Now call SetupDiGetDeviceInterfaceDetail() a second time to receive the device path in the structure at pUnmanagedDetailedInterfaceDataStructure.
                            if (SetupDiGetDeviceInterfaceDetail(DeviceInfoTable, ref InterfaceDataStructure, pUnmanagedDetailedInterfaceDataStructure, StructureSize, IntPtr.Zero, IntPtr.Zero))
                            {
                                //Need to extract the path information from the unmanaged "structure".  The path starts at (pUnmanagedDetailedInterfaceDataStructure + sizeof(DWORD)).
                                IntPtr pToDevicePath = new IntPtr((uint)pUnmanagedDetailedInterfaceDataStructure.ToInt32() + 4);  //Add 4 to the pointer (to get the pointer to point to the path, instead of the DWORD cbSize parameter)
                                DevicePath = Marshal.PtrToStringUni(pToDevicePath); //Now copy the path information into the globally defined DevicePath String.

                                //We now have the proper device path, and we can finally use the path to open I/O handle(s) to the device.
                                SetupDiDestroyDeviceInfoList(DeviceInfoTable);	//Clean up the old structure we no longer need.
                                Marshal.FreeHGlobal(pUnmanagedDetailedInterfaceDataStructure);  //No longer need this unmanaged SP_DEVICE_INTERFACE_DETAIL_DATA buffer.  We already extracted the path information.
                                return true;    //Returning the device path in the global DevicePath String
                            }
                            else //Some unknown failure occurred
                            {
                                uint ErrorCode = (uint)Marshal.GetLastWin32Error();
                                SetupDiDestroyDeviceInfoList(DeviceInfoTable);	//Clean up the old structure.
                                Marshal.FreeHGlobal(pUnmanagedDetailedInterfaceDataStructure);  //No longer need this unmanaged SP_DEVICE_INTERFACE_DETAIL_DATA buffer.  We already extracted the path information.
                                return false;
                            }
                        }

                        InterfaceIndex++;
                        //Keep looping until we either find a device with matching VID and PID, or until we run out of devices to check.
                        //However, just in case some unexpected error occurs, keep track of the number of loops executed.
                        //If the number of loops exceeds a very large number, exit anyway, to prevent inadvertent infinite looping.
                        LoopCounter++;
                        if (LoopCounter == 10000000)	//Surely there aren't more than 10 million devices attached to any forseeable PC...
                        {
                            return false;
                        }
                    }//end of while(true)
                }
                return false;
            }//end of try
            catch
            {
                //Something went wrong if PC gets here.  Maybe a Marshal.AllocHGlobal() failed due to insufficient resources or something.
                return false;
            }
        }
        public bool ReadFileManagedBuffer(SafeFileHandle hFile, byte[] INBuffer, uint nNumberOfBytesToRead, ref uint lpNumberOfBytesRead, IntPtr lpOverlapped)
        {
            IntPtr pINBuffer = IntPtr.Zero;

            try
            {
                pINBuffer = Marshal.AllocHGlobal((int)nNumberOfBytesToRead);    //Allocate some unmanged RAM for the receive data buffer.

                if (ReadFile(hFile, pINBuffer, nNumberOfBytesToRead, ref lpNumberOfBytesRead, lpOverlapped))
                {
                    Marshal.Copy(pINBuffer, INBuffer, 0, (int)lpNumberOfBytesRead);    //Copy over the data from unmanged memory into the managed byte[] INBuffer
                    Marshal.FreeHGlobal(pINBuffer);
                    return true;
                }
                else
                {
                    Marshal.FreeHGlobal(pINBuffer);
                    return false;
                }

            }
            catch
            {
                if (pINBuffer != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(pINBuffer);
                }
                return false;
            }
        }


        string macid;
        string validity;
        int id;
        string servername;
        string dbname;
        string username;
        string password;
        string ip;
        string port;
        string location;
        public static SqlConnection conn;
        public Login()
        {
            if (System.Diagnostics.Process.GetProcessesByName("TNT").Count() > 1)
            {
                MsgBox.Show("One exe is already running, so please close it", "Error", MsgBox.Buttons.OK, MsgBox.Icon.Error, MsgBox.AnimateStyle.FadeIn);
                logflag = 1;
                this.Close();
                return;
            }
            else
            {
                logflag = 0;
                InitializeComponent();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Login.conn.Close();
            Application.Exit();
        }

        DateTime t;
        string isactive;
        string role;
        string current;
        private void button1_Click(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            if (!bLicenseFound)
            {
                MsgBox.Show("License Not Found", "Error", MsgBox.Buttons.OK, MsgBox.Icon.Error, MsgBox.AnimateStyle.FadeIn);
            }
            else
            {
                DateTime d1 = System.DateTime.Today.Date;
                SqlCommand cmdz = new SqlCommand("select * from tbl_company_details", Login.conn);
                SqlDataReader drz = cmdz.ExecuteReader();
                if (drz.HasRows)
                {
                    drz.Close();
                    if (string.IsNullOrEmpty(ex_username.Text) || string.IsNullOrEmpty(ex_password.Text))
                    {
                        MsgBox.Show("Enter Login details", "Error", MsgBox.Buttons.OK, MsgBox.Icon.Error, MsgBox.AnimateStyle.FadeIn);
                        comboBox2.Focus();
                    }
                    else if (comboBox2.SelectedIndex == -1)
                    {
                        MsgBox.Show("Please select company name", "Error", MsgBox.Buttons.OK, MsgBox.Icon.Error, MsgBox.AnimateStyle.FadeIn);
                        comboBox2.Focus();
                    }
                    else if (ex_username.Text == "sun" && ex_password.Text == "sun@123#")
                    {
                        string role = "Super Admin";
                        string user = "sun";
                        string cmpy = comboBox2.SelectedItem.ToString();
                        SqlCommand cmd = new SqlCommand("insert into tbl_SuperAdmin(DateAndTime,Action,Remarks,company_id) values(convert(varchar,'" + DateTime.Now.ToString("dd-MM-yyyy hh:mm:ss tt") + "',103),'" + user + " logged in the system','Login Successfully','" + cmpyid + "')", Login.conn);
                        cmd.ExecuteNonQuery();
                        Home home = new Home(role, user, cmpy);
                        this.Hide();
                        home.ShowDialog();
                    }
                    else
                    {
                        SqlCommand cc = new SqlCommand("select * from tbl_user_details where user_name='" + ex_username.Text + "' and user_password='" + ex_password.Text + "' and company_id='" + cmpyid + "'", conn);
                        SqlDataReader dxx = cc.ExecuteReader();
                        if (dxx.HasRows)
                        {
                            dxx.Close();
                            SqlCommand cx = new SqlCommand("select isactive from tbl_user_details where user_name='" + ex_username.Text + "' and user_password='" + ex_password.Text + "' and company_id='" + cmpyid + "'", conn);
                            SqlDataReader drx = cx.ExecuteReader();
                            while (drx.Read())
                            {
                                isactive = drx[0].ToString();
                            }
                            drx.Close();
                            if (isactive == "True")
                            {
                                SqlCommand cmd = new SqlCommand("select expirydate from tbl_user_details where user_name='" + ex_username.Text + "' and company_id='" + cmpyid + "' and user_password='" + ex_password.Text + "'", conn);
                                SqlDataReader dr = cmd.ExecuteReader();
                                while (dr.Read())
                                {
                                    t = Convert.ToDateTime(dr[0].ToString());
                                }
                                dr.Close();
                                int res = DateTime.Compare(t, d1);
                                if (res >= 0)
                                {
                                    cmd = new SqlCommand("select user_role from tbl_user_details where user_name='" + ex_username.Text + "'", Login.conn);
                                    SqlDataReader d = cmd.ExecuteReader();
                                    while (d.Read())
                                    {
                                        role = d[0].ToString(); ;
                                    }
                                    d.Close();
                                    string uname = ex_username.Text;
                                    string cmpy = comboBox2.SelectedItem.ToString();
                                    cmd = new SqlCommand("insert into tbl_AuditReport(DateAndTime,Action,Remarks,company_id) values(convert(varchar,'" + DateTime.Now.ToString("dd-MM-yyyy hh:mm:ss tt") + "',103),'" + uname + " logged in the system','Login Successfully','" + cmpyid + "')", Login.conn);
                                    cmd.ExecuteNonQuery();
                                    current = AppDomain.CurrentDomain.BaseDirectory;
                                    using (var m = new MemoryStream())
                                    {
                                        pictureBox2.Image.Save(m, ImageFormat.Jpeg);
                                        var img = Image.FromStream(m);
                                        img.Save($@"{current}Images\companylogo.jpg");
                                    }

                                    Home home = new Home(role, uname, cmpy);
                                    this.Hide();
                                    home.ShowDialog();
                                }
                                else
                                {
                                    MsgBox.Show("User account is expired", "Error", MsgBox.Buttons.OK, MsgBox.Icon.Error, MsgBox.AnimateStyle.FadeIn);
                                    comboBox2.Focus();
                                }
                            }
                            else
                            {
                                MsgBox.Show("User account is not active", "Error", MsgBox.Buttons.OK, MsgBox.Icon.Error, MsgBox.AnimateStyle.FadeIn);
                                comboBox2.Focus();
                            }
                        }
                        else
                        {
                            MsgBox.Show("Username or Password or Company selected details are not true", "Error", MsgBox.Buttons.OK, MsgBox.Icon.Error, MsgBox.AnimateStyle.FadeIn);
                            comboBox2.Focus();
                        }
                        dxx.Close();
                    }
                }
                else
                {
                    drz.Close();
                    if (ex_username.Text == "sun" && ex_password.Text == "sun@123#")
                    {
                        string role = "Super Admin";
                        string user = "sun";
                        //string cmpy = comboBox2.SelectedItem.ToString();
                        SqlCommand cmd = new SqlCommand("insert into tbl_SuperAdmin(DateAndTime,Action,Remarks) values(convert(varchar,'" + DateTime.Now.ToString("dd-MM-yyyy hh:mm:ss tt") + "',103),'" + user + " logged in the system','Login Successfully')", Login.conn);
                        cmd.ExecuteNonQuery();
                        Home home = new Home(role, user);
                        this.Hide();

                        home.ShowDialog();
                    }
                    else
                    {
                        MsgBox.Show("Contact Vendor for entering company details", "Error", MsgBox.Buttons.OK, MsgBox.Icon.Error, MsgBox.AnimateStyle.FadeIn);
                    }
                }
            }
        }
        string lic_type;
        private void Login_Load(object sender, EventArgs e)
        {
            label6.Text = "Ver." + Assembly.GetExecutingAssembly().GetName().Version.ToString();
          //  MessageBox.Show(label6.Text);
            if (logflag == 0)
            {
               // checklicense();
                using (FileStream fs = new FileStream("dbconfig.xml", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    DataSet ds = new DataSet();
                    ds.ReadXml(fs);
                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        id = Convert.ToInt32(ds.Tables[0].Rows[i].ItemArray[0]);
                        if (id == 0)
                        {
                            servername = ds.Tables[0].Rows[i].ItemArray[1].ToString();
                            dbname = ds.Tables[0].Rows[i].ItemArray[2].ToString();
                            username = ds.Tables[0].Rows[i].ItemArray[3].ToString();
                            password = ds.Tables[0].Rows[i].ItemArray[4].ToString();
                            lic_type = ds.Tables[0].Rows[i].ItemArray[6].ToString();
                        }
                        else
                        {
                            ip = ds.Tables[0].Rows[i].ItemArray[1].ToString();
                            port = ds.Tables[0].Rows[i].ItemArray[2].ToString();
                            username = ds.Tables[0].Rows[i].ItemArray[3].ToString();
                            password = ds.Tables[0].Rows[i].ItemArray[4].ToString();
                            lic_type = ds.Tables[0].Rows[i].ItemArray[6].ToString();
                        }

                    }

                }
                if (id == 0)
                {

                    string connectionstring = $"Data Source={servername};Initial Catalog = {dbname}; User ID = {username}; Password = {password};MultipleActiveResultSets=True";
                  //  string connectionstring = $"Data Source=192.168.2.9;Initial Catalog =trackntrace; User ID =sa; Password =SQL;MultipleActiveResultSets=True";
                    conn = new SqlConnection(connectionstring);
                    SqlConnection.ClearAllPools();

                }
                else
                {
          
                    string connectionstring = $"Data Source={ip},{port};Network Library = DBMSSOCN; Initial Catalog = {dbname};User ID = {username}; Password = {password};MultipleActiveResultSets=True";
                    conn = new SqlConnection(connectionstring);
                    SqlConnection.ClearAllPools();
                }
                if (lic_type == "SW")
                {
                    checkswlic();
                }
                else if (lic_type == "HW")
                {
                    checkhwlic();
                }

                else if (lic_type == "REG")
                {
                    checkreglic();
                }
                else
                {
                    bLicenseFound = false;
                    licensetextbox.Text = "LICENSE NOT FOUND";
                }
                try
                {
                    conn.Open();
                    conn.Close();
                    Login.conn.Open();
                    SqlCommand cmd = new SqlCommand("select * from tbl_company_details", Login.conn);
                    SqlDataReader dr = cmd.ExecuteReader();
                    if (dr.HasRows)
                    {
                        dr.Close();
                        SqlCommand cmd1 = new SqlCommand("select company_name from tbl_company_details", Login.conn);
                        SqlDataReader dr1 = cmd1.ExecuteReader();
                        while (dr1.Read())
                        {
                            comboBox2.Items.Add(dr1["company_name"].ToString());
                        }
                        dr1.Close();
                        comboBox2.SelectedIndex = 0;
                        ex_username.Focus();

                    }
                    else
                    {
                        dr.Close();
                        comboBox2.Focus();
                    }
                }
                catch
                {
                    MsgBox.Show("Failed to Connect Server", "Error", MsgBox.Buttons.OK, MsgBox.Icon.Error, MsgBox.AnimateStyle.FadeIn);
                    Application.Exit();
                }
            }
        }
        bool bLicenseFound;

        string regmac;
        string regvalid;
        private void checkreglic()
        {
            int regID;

            RegistryKey key = Registry.CurrentUser.OpenSubKey("SUNTEK");
            if (key != null)
            {
                regmac = key.GetValue("mac").ToString();
                regvalid = key.GetValue("validity").ToString();

            }
            NetworkInterface[] networkInterface = NetworkInterface.GetAllNetworkInterfaces();
            //sysDetails.Text =  sysDetails.Text + " total number of interface : " + networkInterface.Length + "\r\n";
            for (int i = 0; i < networkInterface.Length; i++)
            {
                if (networkInterface[i].NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    PhysicalAddress phyAddress = networkInterface[i].GetPhysicalAddress();

                    regID = regmac.CompareTo(phyAddress.ToString());
                    Console.WriteLine("mac id registered: " + regID);
                    if (regID == 0)
                    {
                        string nowdate = DateTime.Now.ToString("dd-MM-yyyy");
                        int res = DateTime.Compare(Convert.ToDateTime(nowdate), Convert.ToDateTime(regvalid));
                        if (res >= 0)
                        {
                            bLicenseFound = false;
                            licensetextbox.Text = "YOUR LICENSE EXPIRED";
                        }
                        else
                        {
                            bLicenseFound = true;
                            licensetextbox.Text = "LICENSE CONNECTED";
                        }
                    }
                    else
                    {
                        bLicenseFound = false;
                        licensetextbox.Text = "LICENSE NOT FOUND";
                    }

                }
            }
        }

        private void checkhwlic()
        {
            bLicenseFound = CheckLicense();
        }

        private void checkswlic()
        {
            #region encryption
            #region encrypt
            Security.CypherPassWord = "123";
            Security.Decrypt("SunConfig.xml", "Dec_SunConfig.xml", "123");

            #endregion
            #region getdata
            using (var fileStream = new FileStream("Dec_SunConfig.xml", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                DataSet ds = new DataSet();
                ds.ReadXml(fileStream);
                int i = 0;
                for (i = 0; i <= ds.Tables[0].Rows.Count - 1; i++)
                {
                    //cameraid = Convert.ToString(ds.Tables[0].Rows[i].ItemArray[0]);
                    macid = Convert.ToString(ds.Tables[0].Rows[i].ItemArray[1]);
                    validity = Convert.ToString(ds.Tables[0].Rows[i].ItemArray[2]);

                }
            }
            File.Delete("Dec_SunConfig.xml");
            #endregion

            #endregion
            string machinemacid = GetMacAddress().ToString();
            string regmacid = macid;
            if (machinemacid.Equals(regmacid))
            {
                string nowdate = DateTime.Now.ToString("dd-MM-yyyy");
                int res = DateTime.Compare(Convert.ToDateTime(nowdate), Convert.ToDateTime(validity));
                if (res >= 0)
                {
                    bLicenseFound = false;
                    licensetextbox.Text = "YOUR LICENSE EXPIRED";
                }
                else
                {
                    bLicenseFound = true;
                    licensetextbox.Text = "LICENSE CONNECTED";
                }


            }
            else
            {
                bLicenseFound = false;
                licensetextbox.Text = "LICENSE NOT FOUND";
            }
        }

        public bool CheckLicense()
        {
            //Register for WM_DEVICECHANGE notifications.  This code uses these messages to detect plug and play connection/disconnection events for USB devices
            DEV_BROADCAST_DEVICEINTERFACE DeviceBroadcastHeader = new DEV_BROADCAST_DEVICEINTERFACE();
            DeviceBroadcastHeader.dbcc_devicetype = DBT_DEVTYP_DEVICEINTERFACE;
            DeviceBroadcastHeader.dbcc_size = (uint)Marshal.SizeOf(DeviceBroadcastHeader);
            DeviceBroadcastHeader.dbcc_reserved = 0;	//Reserved says not to use...
            DeviceBroadcastHeader.dbcc_classguid = InterfaceClassGuid;

            //Need to get the address of the DeviceBroadcastHeader to call RegisterDeviceNotification(), but
            //can't use "&DeviceBroadcastHeader".  Instead, using a roundabout means to get the address by 
            //making a duplicate copy using Marshal.StructureToPtr().
            IntPtr pDeviceBroadcastHeader = IntPtr.Zero;  //Make a pointer.
            pDeviceBroadcastHeader = Marshal.AllocHGlobal(Marshal.SizeOf(DeviceBroadcastHeader)); //allocate memory for a new DEV_BROADCAST_DEVICEINTERFACE structure, and return the address 
            Marshal.StructureToPtr(DeviceBroadcastHeader, pDeviceBroadcastHeader, false);  //Copies the DeviceBroadcastHeader structure into the memory already allocated at DeviceBroadcastHeaderWithPointer
            RegisterDeviceNotification(Handle, pDeviceBroadcastHeader, DEVICE_NOTIFY_WINDOW_HANDLE);


            //Now make an initial attempt to find the USB device, if it was already connected to the PC and enumerated prior to launching the application.
            //If it is connected and present, we should open read and write handles to the device so we can communicate with it later.
            //If it was not connected, we will have to wait until the user plugs the device in, and the WM_DEVICECHANGE callback function can process
            //the message and again search for the device.
            if (CheckIfPresentAndGetUSBDevicePath())	//Check and make sure at least one device with matching VID/PID is attached
            {
                uint ErrorStatusWrite;
                uint ErrorStatusRead;


                //We now have the proper device path, and we can finally open read and write handles to the device.
                WriteHandleToUSBDevice = CreateFile(DevicePath, GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
                ErrorStatusWrite = (uint)Marshal.GetLastWin32Error();
                ReadHandleToUSBDevice = CreateFile(DevicePath, GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
                ErrorStatusRead = (uint)Marshal.GetLastWin32Error();

                if ((ErrorStatusWrite == ERROR_SUCCESS) && (ErrorStatusRead == ERROR_SUCCESS))
                {
                    AttachedState = true;		//Let the rest of the PC application know the USB device is connected, and it is safe to read/write to it
                    AttachedButBroken = false;
                    //sysDetails.Text = "Device Found, AttachedState = TRUE";
                }
                else //for some reason the device was physically plugged in, but one or both of the read/write handles didn't open successfully...
                {
                    AttachedState = false;		//Let the rest of this application known not to read/write to the device.
                    AttachedButBroken = true;	//Flag so that next time a WM_DEVICECHANGE message occurs, can retry to re-open read/write pipes
                    if (ErrorStatusWrite == ERROR_SUCCESS)
                        WriteHandleToUSBDevice.Close();
                    if (ErrorStatusRead == ERROR_SUCCESS)
                        ReadHandleToUSBDevice.Close();
                }
            }
            else	//Device must not be connected (or not programmed with correct firmware)
            {
                AttachedState = false;
                AttachedButBroken = false;
            }

            if (AttachedState == true)
            {
                licensetextbox.Text = "License Connected";
            }
            else
            {
                licensetextbox.Text = "License Not Found";//"Dongle Status: Device not found, verify connect/correct firmware";
            }
            String macIDreg = "";
            if (AttachedState == true)	//Do not try to use the read/write handles unless the USB device is attached and ready
            {
                Byte[] INBuffer = new byte[65];		//Allocate a memory buffer equal to the IN endpoint size + 1
                Byte[] OUTBuffer = new byte[65];	//Allocate a memory buffer equal to the OUT endpoint size + 1
                uint BytesWritten = 0;
                uint BytesRead = 0;
                INBuffer[0] = 0;
                //Get the pushbutton state from the microcontroller firmware.
                OUTBuffer[0] = 0;			//The first byte is the "Report ID" and does not get sent over the USB bus.  Always set = 0.
                OUTBuffer[1] = 0x81;		//0x81 is the "Get Pushbutton State" command in the firmware
                for (uint i = 2; i < 65; i++)	//This loop is not strictly necessary.  Simply initializes unused bytes to
                    OUTBuffer[i] = 0xFF;				//0xFF for lower EMI and power consumption when driving the USB cable.

                //To get the pushbutton state, first, we send a packet with our "Get Pushbutton State" command in it.
                String readString = " ";

                if (WriteFile(WriteHandleToUSBDevice, OUTBuffer, 65, ref BytesWritten, IntPtr.Zero))	//Blocking function, unless an "overlapped" structure is used
                {
                    //Now get the response packet from the firmware.
                    if (ReadFileManagedBuffer(ReadHandleToUSBDevice, INBuffer, 65, ref BytesRead, IntPtr.Zero))		//Blocking function, unless an "overlapped" structure is used	
                    {
                        //INBuffer[0] is the report ID, which we don't care about.
                        //INBuffer[1] is an echo back of the command (see microcontroller firmware).
                        //INBuffer[2] and INBuffer[3] contains the ADC value (see microcontroller firmware).  
                        //if (INBuffer[1] == 0x37)
                        //{
                        readString = "\r\n Registration key: ";
                        //licenseKey = "";
                        for (int i = 4; i < 20; i++)
                        {
                            readString = readString + INBuffer[i];	//Need to reformat the data from two unsigned chars into one unsigned int.
                            //licenseKey = licenseKey + INBuffer[i];
                        }
                        readString = readString + "\r\n Registration Date: ";
                        //registrationDate = "";
                        for (int i = 20; i < 23; i++)
                        {
                            readString = readString + INBuffer[i];// +"\\";	//Need to reformat the data from two unsigned chars into one unsigned int.
                                                                  //registrationDate = registrationDate + INBuffer[i];

                            //if (INBuffer[i] > 9)
                            //{
                            //    registrationDate = registrationDate + Convert.ToString(INBuffer[i]);
                            //}
                            //else
                            //{
                            //    registrationDate = registrationDate + "0" + Convert.ToString(INBuffer[i]);
                            //}
                            if (i != 22)
                            {
                                readString = readString + "/";
                                //  registrationDate = registrationDate + "/";
                            }
                        }

                        readString = readString + INBuffer[23];
                        int registerDate = 0;
                        int registerYear = 0;
                        int registerMonth = 0;
                        registerDate = INBuffer[20];
                        registerMonth = INBuffer[21];
                        registerYear = INBuffer[22] * 100 + INBuffer[23];
                        if (registerDate > 9 && registerMonth > 9)
                        {
                            //registrationDate = Convert.ToString(registerDate) + "/" + Convert.ToString(registerMonth) + "/" + Convert.ToString(registerYear);
                        }
                        else if (registerDate > 9 && registerMonth < 10)
                        {
                            //registrationDate = Convert.ToString(registerDate) + "/" + "0" + Convert.ToString(registerMonth) + "/" + Convert.ToString(registerYear);
                        }
                        else if (registerDate < 10 && registerMonth > 9)
                        {
                            //registrationDate = "0" + Convert.ToString(registerDate) + "/" + Convert.ToString(registerMonth) + "/" + Convert.ToString(registerYear);
                        }
                        else if (registerDate < 10 && registerMonth < 10)
                        {
                            //registrationDate = "0" + Convert.ToString(registerDate) + "/" + "0" + Convert.ToString(registerMonth) + "/" + Convert.ToString(registerYear);
                        }
                        // registrationDate = registrationDate + INBuffer[23];

                        DateTime currentDateTime = DateTime.Today;
                        //MessageBox.Show("Registration date: " + registerYear.ToString() + "/ " + registerMonth.ToString() + "/" + registerDate.ToString());
                        DateTime regDate = new DateTime(registerYear, registerMonth, registerDate);

                        // regDate = DateTime.ParseExact(registrationDate, "dd/MM/yyyy", null).Date;
                        // regDate = DateTime.ParseExact(registrationDate, "MM/dd/yyyy", null).Date;
                        Console.WriteLine("\n system Date Time (Today):  " + currentDateTime + " regDate: " + regDate);
                        int trialLicValidity = (currentDateTime - regDate).Days;
                        if (trialLicValidity > 0)
                        {
                            //Console.WriteLine("\n trial license: (> 0):: " + trialLicValidity + " :: Reg. Date: " + registrationDate + " Today Date: " + currentDateTime);
                            // readString = readString + "\n trial license: (> 0):: " + trialLicValidity + " :: Reg. Date: " + registrationDate + " Today Date: " + currentDateTime;
                        }
                        else if (trialLicValidity < 0)
                        {
                            //Console.WriteLine("\n trial license:  (< 0):: " + trialLicValidity + " :: Reg. Date: " + registrationDate + " Today Date: " + currentDateTime);
                        }
                        else
                        {
                            //Console.WriteLine("\n trial license registered........ start of trial period ");
                        }
                        readString = readString + " \r\n Registration Type: ";
                        int licenseType = 0;
                        for (int i = 24; i < 26; i++)
                        {
                            if (i == 24 && INBuffer[24] == 1)
                            {
                                readString = readString + " : Trial Version";
                                licenseType = 1;

                            }
                            else if (i == 24 && INBuffer[24] == 2)
                            {
                                readString = readString + " : Full Version";
                                noOfDays = 32000;
                                licenseType = 2;
                            }
                            if (i == 25 && INBuffer[24] == 1)
                            {
                                readString = readString + "\r\n No of days: " + INBuffer[i];
                                noOfDays = INBuffer[i];
                            }
                            //readString = readString + " :: " + INBuffer[i];	//Need to reformat the data from two unsigned chars into one unsigned int.
                        }
                        if (licenseType == 1)
                        {
                            if (trialLicValidity >= noOfDays || trialLicValidity < 0)
                            {
                                licensetextbox.Text = "License Expired";
                                bLicExpire = true;
                            }
                            else
                            {
                                bLicExpire = false;
                                //readString = readString + "\n License is valid for " + (noOfDays - trialLicValidity).ToString() + " Days ( license validity = " + noOfDays + ")";
                                licensetextbox.Text = "License Connected";
                            }
                        }
                        else if (licenseType == 2)
                        {
                            bLicExpire = false;
                        }
                        if (INBuffer[25] > 0) /// no of days
                        {
                            //licenseOk = true;
                        }

                        readString = readString + "\r\n Registered UID: ";

                        for (int i = 26; i < 38; i++)
                        {
                            readString = readString + Convert.ToChar(INBuffer[i]);// +"\\";	//Need to reformat the data from two unsigned chars into one unsigned int.
                            macIDreg = macIDreg + Convert.ToChar(INBuffer[i]);
                        }
                        //}
                        //PushbuttonPressed = true;
                    }
                }
                //sysDetails.Text = " Registration Details :: " + readString;

            }
            int regID = -1;
            bRegisteredLic = false;
            if (AttachedState == true)
            {
                NetworkInterface[] networkInterface = NetworkInterface.GetAllNetworkInterfaces();
                //sysDetails.Text =  sysDetails.Text + " total number of interface : " + networkInterface.Length + "\r\n";
                for (int i = 0; i < networkInterface.Length; i++)
                {
                    if (networkInterface[i].NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                    {
                        PhysicalAddress phyAddress = networkInterface[i].GetPhysicalAddress();
                        //sysDetails.Text = sysDetails.Text + " ID: " + networkInterface[i].Id + "\r\n";
                        // sysDetails.Text = sysDetails.Text + " Name: " + networkInterface[i].Name + "\r\n";
                        //sysDetails.Text = sysDetails.Text + " \r\n System UID " + i + ": " + phyAddress.ToString();// +": interface type: " + networkInterface[i].NetworkInterfaceType.ToString() + "\r\n";
                        regID = macIDreg.CompareTo(phyAddress.ToString());
                        Console.WriteLine("mac id registered: " + regID);
                        if (regID == 0)
                        {
                            bRegisteredLic = true;
                        }
                        //int countIP = 0;
                        //foreach (UnicastIPAddressInformation ip in networkInterface[i].GetIPProperties().UnicastAddresses)
                        //{
                        //    if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        //    {
                        //        if (ip.Address.GetAddressBytes()[0] != 169)
                        //        {
                        //            //sysDetails.Text = sysDetails.Text + " " + countIP.ToString() + ": " + ip.Address.ToString() + "\r\n";
                        //        }
                        //        countIP++;
                        //        //Console.WriteLine(ip.Address.ToString());
                        //    }
                        //}
                    }
                }
            }
            if (bLicExpire)
            {
                bRegisteredLic = false;
            }
            return bRegisteredLic;
        }

        public int noOfDays;

        private void checklicense()
        {
            #region encryption
            #region encrypt
            Security.CypherPassWord = "123";
            Security.Decrypt("SunConfig.xml", "Dec_SunConfig.xml", "123");

            #endregion
            #region getdata
            using (var fileStream = new FileStream("Dec_SunConfig.xml", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                DataSet ds = new DataSet();
                ds.ReadXml(fileStream);
                int i = 0;
                for (i = 0; i <= ds.Tables[0].Rows.Count - 1; i++)
                {
                    //cameraid = Convert.ToString(ds.Tables[0].Rows[i].ItemArray[0]);
                    macid = Convert.ToString(ds.Tables[0].Rows[i].ItemArray[1]);
                    validity = Convert.ToString(ds.Tables[0].Rows[i].ItemArray[2]);

                }
            }
            File.Delete("Dec_SunConfig.xml");
            #endregion

            #endregion

            string machinemacid = GetMacAddress().ToString();
            string regmacid = macid;
            if (machinemacid.Equals(regmacid))
            {
                string nowdate = DateTime.Now.ToString("dd-MM-yyyy");
                int res = DateTime.Compare(Convert.ToDateTime(nowdate), Convert.ToDateTime(validity));
                if (res >= 0)
                {
                    bLicenseFound = false;
                   licensetextbox.Text = "YOUR LICENSE EXPIRED";
                }
                else
                {
                    bLicenseFound = true;
                    licensetextbox.Text = "LICENSE CONNECTED";
                }


            }
            else
            {
                bLicenseFound = false;
                licensetextbox.Text = "LICENSE NOT FOUND";
            }
        }
        public static PhysicalAddress GetMacAddress()
        {
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                // Only consider Ethernet network interfaces
                if (nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet &&
                    nic.OperationalStatus == OperationalStatus.Up)
                {
                    return nic.GetPhysicalAddress();
                }
            }
            return null;
        }
        private void button1_Paint(object sender, PaintEventArgs e)
        {
            ControlPaint.DrawBorder(e.Graphics, button1.ClientRectangle,
            SystemColors.ControlLightLight, 5, ButtonBorderStyle.Outset,
            SystemColors.ControlLightLight, 5, ButtonBorderStyle.Outset,
            SystemColors.ControlLightLight, 5, ButtonBorderStyle.Outset,
            SystemColors.ControlLightLight, 5, ButtonBorderStyle.Outset);
        }

        private void button2_Paint(object sender, PaintEventArgs e)
        {
            ControlPaint.DrawBorder(e.Graphics, button2.ClientRectangle,
            SystemColors.ControlLightLight, 5, ButtonBorderStyle.Outset,
            SystemColors.ControlLightLight, 5, ButtonBorderStyle.Outset,
            SystemColors.ControlLightLight, 5, ButtonBorderStyle.Outset,
            SystemColors.ControlLightLight, 5, ButtonBorderStyle.Outset);
        }
        string tempname;
        int logflag = 0;
        string errorline;
        private void button3_Click(object sender, EventArgs e)
        {
            //string current1 = AppDomain.CurrentDomain.BaseDirectory;
            //SqlCommand cmdxx = new SqlCommand("select * from tbl_Templates", Login.conn);
            //SqlDataReader drx = cmdxx.ExecuteReader();
            //while(drx.Read())
            //{
            //    tempname = drx["templatename"].ToString();
            //}
            //drx.Close();
            //string filename1 = @"" + current1 + "testing\\"+tempname+".prn";
            //SqlCommand cmd = new SqlCommand("select template from tbl_Templates where templatename='"+tempname+"'", Login.conn);
            //SqlDataReader dr = cmd.ExecuteReader();
            //if(dr.Read())
            //{

            //    using (StreamWriter sw = new StreamWriter(File.Create(filename1)))
            //    {
            //        sw.Write(dr[0].ToString());
            //        sw.Close();
            //    }
            //}

            /******/
            //ProcessStartInfo startInfo = new ProcessStartInfo();
            //startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
            //startInfo.FileName = "cmd.exe";
            //string mylocation = AppDomain.CurrentDomain.BaseDirectory;
            //startInfo.Arguments = $@"/c net use \\192.168.2.12 && wmic printer get name";
            ////   startInfo.Verb = "runas"; 
            //startInfo.UseShellExecute = false;
            //startInfo.RedirectStandardOutput = true;
            //startInfo.CreateNoWindow = true;

            //using (Process process =Process.Start(startInfo))
            //{
            //    using (StreamReader reader = process.StandardOutput)
            //    {
            //        string result = reader.ReadToEnd();
            //        MessageBox.Show(result);
            //    }
            //}


        }

        public static int cmpyid;
        byte[] getImg;

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            SqlCommand cmd = new SqlCommand("select id,company_logo from tbl_company_details where company_name='"+comboBox2.SelectedItem.ToString()+"'", Login.conn);
            SqlDataReader dr = cmd.ExecuteReader();
            while(dr.Read())
            {
                cmpyid = int.Parse(dr[0].ToString());
                getImg = (byte[])dr["company_logo"];
                if(getImg == null)
                {
                    string imgloc = AppDomain.CurrentDomain.BaseDirectory + @"Images\prodimg.jpg";
                    pictureBox2.Image = new Bitmap(imgloc);
                }
                else
                {
                   
                    MemoryStream mstream = new MemoryStream(getImg);
                    pictureBox2.Image = Image.FromStream(mstream);
                    pictureBox2.SizeMode = PictureBoxSizeMode.StretchImage;
                }
            }
          
            dr.Close();
         
        }

        private void Login_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == (Keys.Alt | Keys.F4))
            {
             //   MessageBox.Show("1");
                e.Handled = true;
            }
            if (e.KeyData == (Keys.Alt))
            {
           //     MessageBox.Show("2");
                e.Handled = true;
            }
        }
    }
}
