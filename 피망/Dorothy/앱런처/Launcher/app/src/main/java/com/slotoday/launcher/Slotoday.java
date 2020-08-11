// launcher  - launcher implementation file
//
// Description      : launcher java file
// Author           : uhrain7761
// Maintainer       : uhrain7761
// How to use       :
// Created          : 2018/07/17
// Last Update      : 2020/05/22
// Known bugs       :
//
// (c) NEOWIZ PLAYSTUDIO Corporation. All rights reserved.
//


package com.slotoday.launcher;

import android.support.v7.app.AppCompatActivity;
import android.os.Bundle;
import android.util.Log;    // 로그 사용
import android.view.View;   // 화면
import android.widget.Button;       // 버튼 사용
import android.widget.CompoundButton;   // 복합의 버튼들
import android.widget.RadioGroup;   // radio group 사용
import android.widget.RadioButton;  // radio button 사용
import android.widget.CheckBox;     // check box 사용
import android.widget.EditText;     // edit text 사용
import android.content.Intent;      // 앱 실행을 위해 필요 (패키지 이름 검색/실행)
// 다이얼로그 띄우기
import android.content.DialogInterface;
import android.app.AlertDialog;

import org.json.JSONException;
import org.json.JSONObject;

// 나중에 구글 정보 필요 할 때
//import com.google.android.gms.appindexing.Action;
//import com.google.android.gms.appindexing.AppIndex;
//import com.google.android.gms.common.api.GoogleApiClient;

public class Slotoday extends AppCompatActivity
{
    private Intent appIntent = null;
    private String packageName = "com.neowiz.casino.slots.classic.slot.machine";
    private String selectServer = "Dev";
    private String isPass = "false";
    private String isSlotPass = "false";
    private String gsn = "";

    //private GoogleApiClient client;     // 나중에 구글 정보 필요 할 때

    @Override
    protected void onCreate(Bundle savedInstanceState)
    {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_slotoday);

        RadioGroup group = (RadioGroup)this.findViewById(R.id.radioGroup); // findViewById : view 에서 해당되는 아이디 찾음
        assert group != null;           // null이 아니어야 한다. 해당 조건을 만족하지 못할 시, exception 발생
        group.setOnCheckedChangeListener(new RadioGroup.OnCheckedChangeListener()   // 해당 그룹에서 체크 이벤트를 받음, build 해보면 override로 onCheckedChanged 메소드 등록
        {
            @Override
            public void onCheckedChanged(RadioGroup rGroup, int checkId)
            {
                RadioButton bt = (RadioButton) rGroup.findViewById(checkId);
                if(bt != null)
                {
                    switch(checkId)
                    {
                        case R.id.Local:
                            selectServer = "Local";
                            break;
                        case R.id.Dev:
                            selectServer = "Dev";
                            break;
                        case R.id.DQ:
                            selectServer = "DQ";
                            break;
                        case R.id.TQ:
                            selectServer = "TQ";
                            break;
                        case R.id.Review:
                            selectServer = "Review";
                            break;
                        case R.id.Live:
                            selectServer = "Live";
                            break;
                    }
                    Log.d("Select Server : ", selectServer);
                }
                else
                {
                    Log.d("RadioButton : ","radio button is empty....");
                }
            }
        });

        CheckBox pass = (CheckBox)this.findViewById(R.id.passCheck);
        assert pass != null;
        pass.setOnCheckedChangeListener(new CompoundButton.OnCheckedChangeListener()
        {
            @Override
            public void onCheckedChanged(CompoundButton button, boolean isCheck)
            {
                isPass = String.valueOf(isCheck);
            }
        });

        CheckBox passSlot = (CheckBox)this.findViewById(R.id.passSlotCheck);
        assert passSlot != null;
        passSlot.setOnCheckedChangeListener(new CompoundButton.OnCheckedChangeListener()
        {
            @Override
            public void onCheckedChanged(CompoundButton button, boolean isCheck)
            {
                isSlotPass = String.valueOf(isCheck);
            }
        });

        final EditText editText = (EditText)this.findViewById(R.id.inputGSN);
        assert editText != null;

        Button startBtn = (Button)this.findViewById(R.id.start);
        assert startBtn != null;
        startBtn.setOnClickListener(new Button.OnClickListener()
        {
            @Override
            public void onClick(View v)
            {
                appIntent = getPackageManager().getLaunchIntentForPackage(packageName);  // 설치된 패키지 앱 가져오기
                if(appIntent != null)
                {
                    JSONObject jsonData = new JSONObject();
                    try
                    {
                        // json data 가공? -> Environment.cs / Initialize() 참조
                        jsonData.put("environment", selectServer);
                        jsonData.put("bypassMaintenance", isPass);
                        jsonData.put("bypassSlotMaintenance", isSlotPass);
                        gsn = editText.getText().toString();
                        jsonData.put("launcherGSN", gsn);
                    }
                    catch (JSONException e)
                    {
                        e.printStackTrace();
                    }
                    // CommonTools.cs / GetIntent() 참조
                    appIntent.putExtra("serverInfo", jsonData.toString());
                    startActivity(appIntent);        // 해당 앱 실행
                }
                else
                {
                    AlertDialog.Builder dialogBuilder = new AlertDialog.Builder(Slotoday.this);  // 다이얼로그 창 띄움
                    dialogBuilder.setPositiveButton("확인", new DialogInterface.OnClickListener()
                    {
                        @Override
                        public void onClick(DialogInterface dialog, int i)
                        {
                            dialog.dismiss();    // 클릭 시, 종료
                        }
                    });
                    dialogBuilder.setMessage("777 Slotoday 앱이 설치되어 있지 않습니다. \n" +  "설치 후 실행해 주시기 바랍니다.");
                    dialogBuilder.show();
                }
            }
        });
        //client = new GoogleApiClient.Builder(this).addApi(AppIndex.API).build();    // 나중에 구글 정보 필요 할 때
    }
}
