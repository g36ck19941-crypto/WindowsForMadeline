/* Self-authored fixed probe. No CRT, GUI, input, audio, network, Steam or target libraries. */
typedef void *HANDLE;
typedef unsigned long DWORD;
typedef int BOOL;
typedef unsigned short WCHAR;
#define API __declspec(dllimport)
API HANDLE __stdcall GetCurrentProcess(void);
API BOOL __stdcall GetProcessMitigationPolicy(HANDLE, int, void *, unsigned long long);
API BOOL __stdcall IsProcessInJob(HANDLE, HANDLE, BOOL *);
API WCHAR *__stdcall GetCommandLineW(void);
API DWORD __stdcall GetCurrentDirectoryW(DWORD, WCHAR *);
API HANDLE __stdcall CreateFileW(const WCHAR *, DWORD, DWORD, void *, DWORD, DWORD, HANDLE);
API BOOL __stdcall WriteFile(HANDLE, const void *, DWORD, DWORD *, void *);
API BOOL __stdcall CloseHandle(HANDLE);
API BOOL __stdcall MoveFileExW(const WCHAR *, const WCHAR *, DWORD);
API void __stdcall Sleep(DWORD);
API void __stdcall ExitProcess(DWORD);

static DWORD wide_length(const WCHAR *s) { DWORD n = 0; while (s[n]) ++n; return n; }
static void append(WCHAR *s, const WCHAR *value) { DWORD n = wide_length(s); while (*value) s[n++] = *value++; s[n] = 0; }
static int same(const char *a, const char *b) { while (*a && *a == *b) { ++a; ++b; } return *a == *b; }
static DWORD text_append(char *s, DWORD n, const char *value) { while (*value) s[n++] = *value++; s[n] = 0; return n; }
static int write_report(WCHAR *directory, const char *id, const char *mode, int complete) {
    static WCHAR final[4096], temporary[4096]; DWORD n = 0, written = 0;
    static char data[512]; HANDLE file;
    final[0] = 0; temporary[0] = 0; append(final, directory);
    append(final, (const WCHAR *)(complete ? L"\\complete.json" : L"\\ready.json"));
    append(temporary, final); append(temporary, (const WCHAR *)L".tmp");
    n = text_append(data, n, "{\"runId\":\""); n = text_append(data, n, id);
    n = text_append(data, n, "\",\"scenario\":\""); n = text_append(data, n, mode);
    n = text_append(data, n, "\",\"stage\":\""); n = text_append(data, n, complete ? "completed" : "ready");
    n = text_append(data, n, "\",\"guiDenied\":true,\"inJob\":true}");
    file = CreateFileW(temporary, 0x40000000, 1, 0, 1, 0x80, 0);
    if (file == (HANDLE)-1) return 0;
    if (!WriteFile(file, data, n, &written, 0) || written != n) { CloseHandle(file); return 0; }
    if (!CloseHandle(file)) return 0;
    return MoveFileExW(temporary, final, 0);
}
void entry(void) {
    WCHAR *command = GetCommandLineW(), *arguments = 0;
    static WCHAR directory[4096]; WCHAR wideId[33];
    const WCHAR *marker = (const WCHAR *)L"--child ";
    char mode[64], id[33]; DWORD i, j = 0, flags = 0, length; BOOL inJob = 0;
    for (i = 0; command[i]; ++i) {
        for (j = 0; marker[j] && command[i+j] == marker[j]; ++j) {}
        if (!marker[j]) { arguments = command+i+j; break; }
    }
    if (!arguments) ExitProcess(41); command = arguments;
    for (i = 0; command[i] && command[i] != ' ' && i < 63; ++i) mode[i] = (char)command[i];
    mode[i] = 0; if (command[i] != ' ') ExitProcess(42); command += i+1;
    for (i = 0; i < 32; ++i) {
        if (!((command[i] >= '0' && command[i] <= '9') || (command[i] >= 'a' && command[i] <= 'f'))) ExitProcess(43);
        id[i] = (char)command[i]; wideId[i] = command[i];
    }
    id[32] = 0; wideId[32] = 0; if (command[32]) ExitProcess(44);
    if (!same(mode,"Complete") && !same(mode,"EarlyExit") && !same(mode,"Hang") && !same(mode,"CloseJobAfterReady") && !same(mode,"ParentFailureAfterReady")) ExitProcess(45);
    if (!GetProcessMitigationPolicy(GetCurrentProcess(), 4, &flags, 4) || !(flags & 1) || (flags & 2)) ExitProcess(46);
    if (!IsProcessInJob(GetCurrentProcess(), 0, &inJob) || !inJob) ExitProcess(47);
    length = GetCurrentDirectoryW(4096, directory); if (!length || length > 3500) ExitProcess(48);
    append(directory, (const WCHAR *)L"\\artifacts\\cdr-082-restricted-process\\runs\\"); append(directory, wideId);
    if (!write_report(directory, id, mode, 0)) ExitProcess(49);
    if (same(mode,"EarlyExit")) ExitProcess(23);
    if (!same(mode,"Complete")) Sleep(30000);
    if (!write_report(directory, id, mode, 1)) ExitProcess(50);
    ExitProcess(0);
}
