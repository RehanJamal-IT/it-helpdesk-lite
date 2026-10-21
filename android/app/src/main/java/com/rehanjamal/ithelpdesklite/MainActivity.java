/*
 * IT Help Desk Lite (Android)  -  free IT ticket app
 * Copyright (c) 2026 Rehan Jamal  -  https://github.com/RehanJamal-IT
 *
 * Employees fill their details once, pick a problem type, describe it,
 * attach photos and press Send. The phone's e-mail app (Gmail / Outlook)
 * opens a ready, structured ticket for the IT team. No server, no internet
 * permission, no data collected by the app.
 *
 * Free to use. Do NOT remove the author credits or publish this app
 * under another name - see LICENSE.
 */
package com.rehanjamal.ithelpdesklite;

import android.app.Activity;
import android.app.AlertDialog;
import android.content.ActivityNotFoundException;
import android.content.ClipData;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.content.pm.PackageInfo;
import android.database.Cursor;
import android.graphics.Color;
import android.graphics.Typeface;
import android.graphics.drawable.GradientDrawable;
import android.net.Uri;
import android.os.Build;
import android.os.Bundle;
import android.provider.OpenableColumns;
import android.text.InputType;
import android.util.Patterns;
import android.util.TypedValue;
import android.view.Gravity;
import android.view.View;
import android.view.ViewGroup;
import android.view.WindowInsets;
import android.widget.ArrayAdapter;
import android.widget.Button;
import android.widget.EditText;
import android.widget.LinearLayout;
import android.widget.RadioButton;
import android.widget.RadioGroup;
import android.widget.ScrollView;
import android.widget.Spinner;
import android.widget.TextView;
import android.widget.Toast;

import java.security.MessageDigest;
import java.util.ArrayList;

public class MainActivity extends Activity {

    // ------------------------------------------------------------ author (please keep)
    static final String APP = "IT Help Desk Lite";
    static final String AUTHOR = "Rehan Jamal";
    static final String AUTHOR_TITLE = "IT Manager / Senior Systems Administrator";
    static final String AUTHOR_YEARS = "16+ years in IT  ·  IT for 450+ users";
    static final String LINKEDIN = "https://www.linkedin.com/in/rehan-jamal/";
    static final String GITHUB = "https://github.com/RehanJamal-IT";
    static final String REPO = "https://github.com/RehanJamal-IT/it-helpdesk-lite";
    static final String CREDIT = "IT Help Desk Lite - free app by Rehan Jamal";

    static final int NAVY = Color.rgb(11, 37, 69);
    static final int GOLD = Color.rgb(245, 180, 0);
    static final int SOFT = Color.rgb(110, 120, 135);
    static final int BACK = Color.rgb(244, 246, 250);
    static final int REQ_FILES = 41;

    static final String[][] KINDS = {
            {"Computer / Laptop", "Hardware"},
            {"Password / Login", "Password / Account"},
            {"Email / Outlook", "Email / Outlook"},
            {"Internet / Wi-Fi", "Network / Internet"},
            {"Printer / Scanner", "Printer / Scanner"},
            {"Software / App", "Software / Install"},
            {"Shared folder / Access", "Access / Permissions"},
            {"Teams / Office 365", "Microsoft 365 / Teams"},
            {"VPN / Remote", "VPN / Remote"},
            {"Mobile phone / Tablet", "Mobile Device"},
            {"Other / New user", "Other"},
            {"Suspicious email / Virus", "Security"},
    };

    SharedPreferences prefs;
    LinearLayout root, content;
    TextView header;
    Button[] tabButtons = new Button[4];
    int currentTab = 0;
    boolean settingsUnlocked = false;
    ArrayList<Uri> files = new ArrayList<>();

    // ticket tab state (kept while switching tabs)
    int kindIndex = 0, levelChoice = 1;
    String draftTitle = "", draftText = "";

    // ============================================================ lifecycle
    @Override
    protected void onCreate(Bundle b) {
        super.onCreate(b);
        prefs = getSharedPreferences("ithelp", MODE_PRIVATE);

        root = new LinearLayout(this);
        root.setOrientation(LinearLayout.VERTICAL);
        root.setBackgroundColor(Color.WHITE);
        // edge-to-edge (Android 15+): keep content away from status / navigation bars
        root.setOnApplyWindowInsetsListener(new View.OnApplyWindowInsetsListener() {
            @Override
            public WindowInsets onApplyWindowInsets(View v, WindowInsets in) {
                v.setPadding(in.getSystemWindowInsetLeft(), in.getSystemWindowInsetTop(),
                        in.getSystemWindowInsetRight(), in.getSystemWindowInsetBottom());
                return in;
            }
        });

        header = new TextView(this);
        header.setTextColor(Color.WHITE);
        header.setBackgroundColor(NAVY);
        header.setTextSize(TypedValue.COMPLEX_UNIT_SP, 21);
        header.setTypeface(Typeface.DEFAULT_BOLD);
        header.setPadding(dp(18), dp(16), dp(18), dp(16));
        root.addView(header, new LinearLayout.LayoutParams(-1, -2));

        LinearLayout tabs = new LinearLayout(this);
        tabs.setBackgroundColor(BACK);
        String[] names = {"New ticket", "My details", "Settings", "About"};
        for (int i = 0; i < 4; i++) {
            final int idx = i;
            Button t = new Button(this);
            t.setText(names[i]);
            t.setAllCaps(false);
            t.setTextSize(TypedValue.COMPLEX_UNIT_SP, 13);
            t.setBackgroundColor(Color.TRANSPARENT);
            t.setPadding(dp(2), 0, dp(2), 0);
            t.setOnClickListener(new View.OnClickListener() {
                public void onClick(View v) { openTab(idx); }
            });
            tabButtons[i] = t;
            tabs.addView(t, new LinearLayout.LayoutParams(0, dp(52), 1f));
        }
        root.addView(tabs, new LinearLayout.LayoutParams(-1, -2));

        content = new LinearLayout(this);
        content.setOrientation(LinearLayout.VERTICAL);
        root.addView(content, new LinearLayout.LayoutParams(-1, 0, 1f));
        setContentView(root);

        refreshHeader();
        if (!profileOk()) {
            openTab(1);
            toast("Welcome! Please enter your details once.");
        } else if (!settingsConfigured()) {
            openTab(2);
            toast("IT: please set where tickets are sent.");
        } else {
            openTab(0);
        }
    }

    // ============================================================ helpers
    int dp(int v) { return Math.round(v * getResources().getDisplayMetrics().density); }

    void toast(String s) { Toast.makeText(this, s, Toast.LENGTH_LONG).show(); }

    String pref(String k, String def) { return prefs.getString(k, def); }

    static boolean emailOk(String e) { return e != null && Patterns.EMAIL_ADDRESS.matcher(e.trim()).matches(); }

    static int digits(String s) { int n = 0; for (char c : s.toCharArray()) if (Character.isDigit(c)) n++; return n; }

    boolean profileOk() {
        return pref("first", "").length() > 0 && pref("dept", "").length() > 0
                && emailOk(pref("email", "")) && digits(pref("phone", "")) >= 7;
    }

    boolean settingsConfigured() {
        String e = pref("l1_email", "");
        return emailOk(e) && !e.toLowerCase().endsWith("@yourcompany.com");
    }

    String company() { return pref("company", "Your Company"); }
    String l1Name() { return pref("l1_name", "Level 1 - Help Desk"); }
    String l2Name() { return pref("l2_name", "Level 2 - IT Manager"); }

    void refreshHeader() { header.setText(company() + " IT Help"); }

    static String hash(String pin) {
        try {
            MessageDigest md = MessageDigest.getInstance("SHA-256");
            byte[] h = md.digest(("ithelp-lite:" + pin).getBytes("UTF-8"));
            StringBuilder sb = new StringBuilder();
            for (byte x : h) sb.append(String.format("%02x", x));
            return sb.toString();
        } catch (Exception e) { return pin; }
    }

    String appVersion() {
        try {
            PackageInfo pi = getPackageManager().getPackageInfo(getPackageName(), 0);
            return pi.versionName;
        } catch (Exception e) { return ""; }
    }

    // ---------- simple UI builders
    LinearLayout page(LinearLayout bottomBar) {
        content.removeAllViews();
        ScrollView sv = new ScrollView(this);
        sv.setFillViewport(true);
        LinearLayout col = new LinearLayout(this);
        col.setOrientation(LinearLayout.VERTICAL);
        col.setPadding(dp(18), dp(12), dp(18), dp(24));
        sv.addView(col, new ViewGroup.LayoutParams(-1, -2));
        content.addView(sv, new LinearLayout.LayoutParams(-1, 0, 1f));
        if (bottomBar != null) content.addView(bottomBar, new LinearLayout.LayoutParams(-1, -2));
        return col;
    }

    TextView text(LinearLayout p, String s, int sp, int color, boolean bold, int topDp) {
        TextView t = new TextView(this);
        t.setText(s);
        t.setTextSize(TypedValue.COMPLEX_UNIT_SP, sp);
        t.setTextColor(color);
        if (bold) t.setTypeface(Typeface.DEFAULT_BOLD);
        LinearLayout.LayoutParams lp = new LinearLayout.LayoutParams(-1, -2);
        lp.topMargin = dp(topDp);
        p.addView(t, lp);
        return t;
    }

    TextView heading(LinearLayout p, String s) { return text(p, s, 15, Color.BLACK, true, 16); }

    EditText field(LinearLayout p, String label, String value, int inputType) {
        text(p, label, 14, Color.DKGRAY, false, 10);
        EditText e = new EditText(this);
        e.setText(value);
        e.setInputType(inputType);
        e.setTextSize(TypedValue.COMPLEX_UNIT_SP, 16);
        p.addView(e, new LinearLayout.LayoutParams(-1, -2));
        return e;
    }

    Button button(String s, int bg, int fg) {
        Button b = new Button(this);
        b.setText(s);
        b.setAllCaps(false);
        b.setTextColor(fg);
        b.setTextSize(TypedValue.COMPLEX_UNIT_SP, 16);
        b.setTypeface(Typeface.DEFAULT_BOLD);
        GradientDrawable g = new GradientDrawable();
        g.setColor(bg);
        g.setCornerRadius(dp(10));
        b.setBackground(g);
        b.setPadding(dp(14), 0, dp(14), 0);
        return b;
    }

    LinearLayout bar(Button... buttons) {
        LinearLayout l = new LinearLayout(this);
        l.setBackgroundColor(BACK);
        l.setPadding(dp(14), dp(10), dp(14), dp(10));
        for (int i = 0; i < buttons.length; i++) {
            LinearLayout.LayoutParams lp = new LinearLayout.LayoutParams(0, dp(54), 1f);
            if (i > 0) lp.leftMargin = dp(10);
            l.addView(buttons[i], lp);
        }
        return l;
    }

    LinearLayout card(LinearLayout p) {
        LinearLayout c = new LinearLayout(this);
        c.setOrientation(LinearLayout.VERTICAL);
        GradientDrawable g = new GradientDrawable();
        g.setColor(BACK);
        g.setCornerRadius(dp(12));
        c.setBackground(g);
        c.setPadding(dp(14), dp(12), dp(14), dp(14));
        LinearLayout.LayoutParams lp = new LinearLayout.LayoutParams(-1, -2);
        lp.topMargin = dp(8);
        p.addView(c, lp);
        return c;
    }

    // ============================================================ tabs
    void openTab(final int i) {
        if (currentTab == 0 && content.getChildCount() > 0) saveDraft();
        if (i == 2 && !settingsUnlocked && pref("pin_hash", "").length() > 0) {
            final EditText pin = new EditText(this);
            pin.setInputType(InputType.TYPE_CLASS_NUMBER | InputType.TYPE_NUMBER_VARIATION_PASSWORD);
            new AlertDialog.Builder(this).setTitle("IT settings PIN").setView(pin)
                    .setPositiveButton("OK", (d, w) -> {
                        if (hash(pin.getText().toString()).equals(pref("pin_hash", ""))) {
                            settingsUnlocked = true; openTab(2);
                        } else toast("Wrong PIN. Only IT can change these settings.");
                    })
                    .setNegativeButton("Cancel", null).show();
            return;
        }
        currentTab = i;
        for (int k = 0; k < 4; k++) {
            tabButtons[k].setTextColor(k == i ? NAVY : SOFT);
            tabButtons[k].setTypeface(k == i ? Typeface.DEFAULT_BOLD : Typeface.DEFAULT);
        }
        if (i == 0) buildTicket();
        else if (i == 1) buildDetails();
        else if (i == 2) buildSettings();
        else buildAbout();
    }

    // ------------------------------------------------------------ tab: new ticket
    Spinner spKind;
    EditText etTitle, etText;
    RadioButton rb1, rb2;
    TextView tvFiles;

    void buildTicket() {
        Button send = button("Send to IT", GOLD, Color.BLACK);
        send.setTextSize(TypedValue.COMPLEX_UNIT_SP, 18);
        send.setOnClickListener(v -> sendTicket());
        LinearLayout col = page(bar(send));

        LinearLayout c = card(col);
        text(c, "Ticket from (your details)", 15, Color.BLACK, true, 0);
        text(c, "Name:  " + (pref("first", "") + " " + pref("last", "")).trim()
                + "\nDepartment:  " + pref("dept", "")
                + (pref("location", "").length() > 0 ? "  ·  " + pref("location", "") : "")
                + "\nMobile:  " + pref("phone", "")
                + "\nE-mail:  " + pref("email", ""), 14, Color.DKGRAY, false, 6);
        text(c, "Device: " + Build.MANUFACTURER + " " + Build.MODEL + "  ·  Android " + Build.VERSION.RELEASE
                + "  (added automatically)", 12, SOFT, false, 6);
        Button edit = button("Change my details", Color.WHITE, NAVY);
        edit.setTextSize(TypedValue.COMPLEX_UNIT_SP, 14);
        edit.setOnClickListener(v -> openTab(1));
        LinearLayout.LayoutParams elp = new LinearLayout.LayoutParams(-2, dp(42));
        elp.topMargin = dp(10);
        c.addView(edit, elp);

        heading(col, "What is the problem about? *");
        spKind = new Spinner(this);
        String[] labels = new String[KINDS.length];
        for (int k = 0; k < KINDS.length; k++) labels[k] = KINDS[k][0];
        ArrayAdapter<String> ad = new ArrayAdapter<>(this, android.R.layout.simple_spinner_item, labels);
        ad.setDropDownViewResource(android.R.layout.simple_spinner_dropdown_item);
        spKind.setAdapter(ad);
        spKind.setSelection(kindIndex);
        col.addView(spKind, new LinearLayout.LayoutParams(-1, dp(52)));

        heading(col, "Short title (optional)");
        etTitle = new EditText(this);
        etTitle.setSingleLine(true);
        etTitle.setText(draftTitle);
        col.addView(etTitle, new LinearLayout.LayoutParams(-1, -2));

        heading(col, "Describe the problem *  (do NOT write your password)");
        etText = new EditText(this);
        etText.setMinLines(4);
        etText.setGravity(Gravity.TOP);
        etText.setInputType(InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_FLAG_MULTI_LINE | InputType.TYPE_TEXT_FLAG_CAP_SENTENCES);
        etText.setText(draftText);
        col.addView(etText, new LinearLayout.LayoutParams(-1, -2));

        heading(col, "Photos / screenshots (optional)");
        tvFiles = text(col, "", 14, SOFT, false, 4);
        LinearLayout fb = new LinearLayout(this);
        Button add = button("Add photos", Color.WHITE, NAVY);
        add.setTextSize(TypedValue.COMPLEX_UNIT_SP, 14);
        add.setOnClickListener(v -> pickFiles());
        Button clear = button("Remove all", Color.WHITE, SOFT);
        clear.setTextSize(TypedValue.COMPLEX_UNIT_SP, 14);
        clear.setOnClickListener(v -> { files.clear(); showFiles(); });
        LinearLayout.LayoutParams a1 = new LinearLayout.LayoutParams(0, dp(46), 1f);
        LinearLayout.LayoutParams a2 = new LinearLayout.LayoutParams(0, dp(46), 1f);
        a2.leftMargin = dp(10);
        fb.addView(add, a1);
        fb.addView(clear, a2);
        LinearLayout.LayoutParams fbl = new LinearLayout.LayoutParams(-1, -2);
        fbl.topMargin = dp(6);
        col.addView(fb, fbl);
        showFiles();

        heading(col, "Who should get this?");
        RadioGroup rg = new RadioGroup(this);
        rb1 = new RadioButton(this);
        rb1.setText(l1Name());
        rb1.setId(View.generateViewId());
        rb2 = new RadioButton(this);
        rb2.setText(l2Name());
        rb2.setId(View.generateViewId());
        rg.addView(rb1);
        rg.addView(rb2);
        (levelChoice == 2 ? rb2 : rb1).setChecked(true);
        col.addView(rg);
        text(col, "Default: goes to " + l1Name() + " with a copy to " + l2Name() + ".", 13, SOFT, false, 2);
    }

    void saveDraft() {
        if (etTitle == null) return;
        draftTitle = etTitle.getText().toString();
        draftText = etText.getText().toString();
        kindIndex = spKind.getSelectedItemPosition();
        levelChoice = rb2.isChecked() ? 2 : 1;
    }

    void showFiles() {
        if (tvFiles == null) return;
        if (files.isEmpty()) { tvFiles.setText("No photos added."); return; }
        StringBuilder sb = new StringBuilder();
        for (Uri u : files) sb.append("•  ").append(fileName(u)).append("\n");
        tvFiles.setText(sb.toString().trim());
    }

    String fileName(Uri u) {
        try (Cursor c = getContentResolver().query(u, null, null, null, null)) {
            if (c != null && c.moveToFirst()) {
                int i = c.getColumnIndex(OpenableColumns.DISPLAY_NAME);
                if (i >= 0) return c.getString(i);
            }
        } catch (Exception ignored) { }
        String s = u.getLastPathSegment();
        return s == null ? "photo" : s;
    }

    void pickFiles() {
        Intent i = new Intent(Intent.ACTION_OPEN_DOCUMENT);
        i.addCategory(Intent.CATEGORY_OPENABLE);
        i.setType("*/*");
        i.putExtra(Intent.EXTRA_MIME_TYPES, new String[]{"image/*", "application/pdf", "text/plain"});
        i.putExtra(Intent.EXTRA_ALLOW_MULTIPLE, true);
        try { startActivityForResult(i, REQ_FILES); }
        catch (ActivityNotFoundException e) { toast("No file picker found on this phone."); }
    }

    @Override
    protected void onActivityResult(int req, int res, Intent data) {
        super.onActivityResult(req, res, data);
        if (req != REQ_FILES || res != RESULT_OK || data == null) return;
        if (data.getClipData() != null) {
            ClipData cd = data.getClipData();
            for (int i = 0; i < cd.getItemCount(); i++) addFile(cd.getItemAt(i).getUri());
        } else if (data.getData() != null) addFile(data.getData());
        showFiles();
    }

    void addFile(Uri u) {
        if (u == null || files.contains(u)) return;
        try { getContentResolver().takePersistableUriPermission(u, Intent.FLAG_GRANT_READ_URI_PERMISSION); }
        catch (Exception ignored) { }
        files.add(u);
    }

    void sendTicket() {
        saveDraft();
        if (!profileOk()) { toast("Please fill your details first."); openTab(1); return; }
        if (!settingsConfigured()) { toast("IT has not set the ticket e-mail addresses yet."); openTab(2); return; }
        String problem = draftText.trim();
        if (problem.length() < 3) { toast("Please describe the problem."); return; }

        String[] kind = KINDS[kindIndex];
        String subject = "[IT-TICKET] " + kind[1] + ": " + (draftTitle.trim().length() > 0 ? draftTitle.trim() : problem);
        if (subject.length() > 150) subject = subject.substring(0, 150);

        String to, cc, toName;
        if (levelChoice == 2) { to = pref("l2_email", ""); cc = ""; toName = l2Name(); }
        else { to = pref("l1_email", ""); cc = pref("l2_email", ""); toName = l1Name(); }

        StringBuilder b = new StringBuilder();
        b.append("Sent to: ").append(toName).append("\n");
        b.append("Name: ").append((pref("first", "") + " " + pref("last", "")).trim()).append("\n");
        b.append("Department: ").append(pref("dept", "")).append("\n");
        b.append("Phone: ").append(pref("phone", "")).append("\n");
        b.append("Email: ").append(pref("email", "")).append("\n");
        if (pref("location", "").length() > 0) b.append("Location: ").append(pref("location", "")).append("\n");
        b.append("Category: ").append(kind[1]).append("\n\n");
        b.append("Problem:\n").append(problem).append("\n\n");
        b.append("---- Device details (automatic) ----\n");
        b.append("Device: ").append(Build.MANUFACTURER).append(" ").append(Build.MODEL).append("\n");
        b.append("Android: ").append(Build.VERSION.RELEASE).append(" (API ").append(Build.VERSION.SDK_INT).append(")\n");
        if (!files.isEmpty()) b.append("Attachments: ").append(files.size()).append("\n");
        b.append("\n-- sent with ").append(CREDIT).append(" (Android) - ").append(REPO.replace("https://", "")).append("\n");

        Intent i;
        if (files.isEmpty()) {
            i = new Intent(Intent.ACTION_SENDTO, Uri.parse("mailto:"));
        } else if (files.size() == 1) {
            i = new Intent(Intent.ACTION_SEND);
            i.setType("message/rfc822");
            i.putExtra(Intent.EXTRA_STREAM, files.get(0));
        } else {
            i = new Intent(Intent.ACTION_SEND_MULTIPLE);
            i.setType("message/rfc822");
            i.putParcelableArrayListExtra(Intent.EXTRA_STREAM, new ArrayList<>(files));
        }
        i.putExtra(Intent.EXTRA_EMAIL, new String[]{to});
        if (cc.length() > 0) i.putExtra(Intent.EXTRA_CC, new String[]{cc});
        i.putExtra(Intent.EXTRA_SUBJECT, subject);
        i.putExtra(Intent.EXTRA_TEXT, b.toString());
        if (!files.isEmpty()) {
            ClipData clip = ClipData.newRawUri("attachment", files.get(0));
            for (int k = 1; k < files.size(); k++) clip.addItem(new ClipData.Item(files.get(k)));
            i.setClipData(clip);
            i.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION);
        }
        try {
            startActivity(Intent.createChooser(i, "Send ticket with (Gmail / Outlook)"));
            draftTitle = ""; draftText = ""; files.clear();
            toast("Your e-mail is ready. Just press SEND.");
            buildTicket();
        } catch (ActivityNotFoundException e) {
            toast("No e-mail app found. Please install Gmail or Outlook.");
        }
    }

    // ------------------------------------------------------------ tab: my details
    void buildDetails() {
        Button save = button("Save my details", NAVY, Color.WHITE);
        LinearLayout col = page(bar(save));
        text(col, "These details are added to every ticket, so IT knows who you are and how to reach you.",
                14, SOFT, false, 4);
        final EditText f = field(col, "First name *", pref("first", ""), InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_FLAG_CAP_WORDS);
        final EditText l = field(col, "Last name", pref("last", ""), InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_FLAG_CAP_WORDS);
        final EditText d = field(col, "Department / project *", pref("dept", ""), InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_FLAG_CAP_WORDS);
        final EditText ph = field(col, "Mobile number *", pref("phone", ""), InputType.TYPE_CLASS_PHONE);
        final EditText em = field(col, "Work e-mail *  (IT replies to this e-mail)", pref("email", ""), InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_VARIATION_EMAIL_ADDRESS);
        final EditText lo = field(col, "Office / site / floor", pref("location", ""), InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_FLAG_CAP_WORDS);
        save.setOnClickListener(v -> {
            String first = f.getText().toString().trim(), dept = d.getText().toString().trim();
            String phone = ph.getText().toString().trim(), email = em.getText().toString().trim();
            if (first.isEmpty() || dept.isEmpty()) { toast("Please fill first name and department."); return; }
            if (digits(phone) < 7 || digits(phone) > 15) { toast("Please enter a valid mobile number."); return; }
            if (!emailOk(email)) { toast("Please check your e-mail, e.g. name@company.com"); return; }
            prefs.edit().putString("first", first).putString("last", l.getText().toString().trim())
                    .putString("dept", dept).putString("phone", phone).putString("email", email)
                    .putString("location", lo.getText().toString().trim()).apply();
            toast("Your details are saved.");
            openTab(settingsConfigured() ? 0 : 2);
        });
    }

    // ------------------------------------------------------------ tab: settings
    void buildSettings() {
        Button save = button("Save settings", NAVY, Color.WHITE);
        LinearLayout col = page(bar(save));
        text(col, "Where should tickets be e-mailed? Fill this once and press Save.\n"
                + "Default: a ticket goes to Level 1, with a copy (CC) to Level 2.", 14, SOFT, false, 4);
        heading(col, "LEVEL 1  -  first-line support");
        final EditText n1 = field(col, "Name shown to users", l1Name(), InputType.TYPE_CLASS_TEXT);
        final EditText e1 = field(col, "E-mail address *", pref("l1_email", ""), InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_VARIATION_EMAIL_ADDRESS);
        heading(col, "LEVEL 2  -  IT Manager");
        final EditText n2 = field(col, "Name shown to users", l2Name(), InputType.TYPE_CLASS_TEXT);
        final EditText e2 = field(col, "E-mail address *", pref("l2_email", ""), InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_VARIATION_EMAIL_ADDRESS);
        heading(col, "COMPANY");
        final EditText co = field(col, "Company name", company(), InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_FLAG_CAP_WORDS);
        heading(col, "PROTECT SETTINGS (optional)");
        final EditText pin = field(col, "Settings PIN - so normal users cannot change the e-mails (empty = no PIN)",
                pref("pin_hash", "").length() > 0 ? "****" : "",
                InputType.TYPE_CLASS_NUMBER | InputType.TYPE_NUMBER_VARIATION_PASSWORD);
        save.setOnClickListener(v -> {
            String a = e1.getText().toString().trim(), c2 = e2.getText().toString().trim();
            if (!emailOk(a)) { toast("Please enter a valid Level 1 e-mail."); return; }
            if (!emailOk(c2)) { toast("Please enter a valid Level 2 e-mail."); return; }
            SharedPreferences.Editor ed = prefs.edit();
            ed.putString("l1_name", n1.getText().toString().trim().isEmpty() ? "Level 1" : n1.getText().toString().trim());
            ed.putString("l2_name", n2.getText().toString().trim().isEmpty() ? "Level 2" : n2.getText().toString().trim());
            ed.putString("l1_email", a).putString("l2_email", c2);
            if (!co.getText().toString().trim().isEmpty()) ed.putString("company", co.getText().toString().trim());
            String p = pin.getText().toString();
            if (p.isEmpty()) ed.putString("pin_hash", "");
            else if (!p.equals("****")) ed.putString("pin_hash", hash(p));
            ed.apply();
            refreshHeader();
            toast("Settings saved. Tickets go to " + a);
            openTab(profileOk() ? 0 : 1);
        });
    }

    // ------------------------------------------------------------ tab: about
    void buildAbout() {
        Button share = button("Share", GOLD, Color.BLACK);
        share.setOnClickListener(v -> shareApp());
        Button gh = button("GitHub", NAVY, Color.WHITE);
        gh.setOnClickListener(v -> openUrl(REPO));
        Button li = button("LinkedIn", Color.rgb(10, 102, 194), Color.WHITE);
        li.setOnClickListener(v -> openUrl(LINKEDIN));
        LinearLayout col = page(bar(li, gh, share));

        text(col, APP, 26, NAVY, true, 6);
        text(col, "Version " + appVersion() + "  ·  Free IT ticket app", 14, SOFT, false, 2);

        LinearLayout c = card(col);
        text(c, "Designed and built by", 13, SOFT, false, 0);
        text(c, AUTHOR, 22, Color.BLACK, true, 2);
        text(c, AUTHOR_TITLE + "\n" + AUTHOR_YEARS, 14, Color.DKGRAY, false, 4);
        link(c, "LinkedIn:  linkedin.com/in/rehan-jamal", LINKEDIN);
        link(c, "GitHub:  github.com/RehanJamal-IT", GITHUB);
        text(c, "Open to IT jobs and freelance IT automation projects.", 13, SOFT, false, 8);

        text(col, "Do you like this app?", 18, Color.BLACK, true, 22);
        text(col, "This app is free. If it helps you, please support my work:\n"
                + "★  Give it a star on GitHub\n"
                + "➤  Share it with friends and colleagues who need a free IT help desk\n"
                + "✉  Send me your feedback or ideas on LinkedIn or GitHub", 14, Color.DKGRAY, false, 6);
        text(col, "© 2026 " + AUTHOR + ". Free to use in any company. Please do not remove the author's name "
                + "or publish this app under another name. See LICENSE on GitHub.\n\n"
                + "Privacy: this app has no internet access and collects no data. Your ticket is sent only "
                + "by your own e-mail app, to the addresses your IT team sets.", 12, SOFT, false, 18);
    }

    void link(LinearLayout p, String label, final String url) {
        TextView t = text(p, label, 14, NAVY, false, 8);
        t.setPaintFlags(t.getPaintFlags() | android.graphics.Paint.UNDERLINE_TEXT_FLAG);
        t.setOnClickListener(v -> openUrl(url));
    }

    void openUrl(String url) {
        try { startActivity(new Intent(Intent.ACTION_VIEW, Uri.parse(url))); }
        catch (ActivityNotFoundException e) { toast(url); }
    }

    void shareApp() {
        Intent i = new Intent(Intent.ACTION_SEND);
        i.setType("text/plain");
        i.putExtra(Intent.EXTRA_TEXT, "Free IT ticket app - employees report IT problems with photos in 1 minute.\n"
                + APP + " by " + AUTHOR + ":\n" + REPO);
        startActivity(Intent.createChooser(i, "Share " + APP));
    }

    @Override
    protected void onPause() {
        super.onPause();
        if (currentTab == 0) saveDraft();
    }
}
