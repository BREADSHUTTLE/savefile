// Fill out your copyright notice in the Description page of Project Settings.


#include "W_UserWidget.h"

void UW_UserWidget::NativeOnInitialized()
{
    Super::NativeOnInitialized();

    resetButton->OnClicked.AddDynamic(this, &UW_UserWidget::ResetCount);
}


void UW_UserWidget::SetChargeTime(float value, float maxValue)
{
    float percent = 0;
    if (value >= maxValue) 
    {
        percent = 1;
    }
    else
    {
        percent = value / maxValue;
        chargingBar->SetPercent(percent);
    }
}

void UW_UserWidget::ShowBar()
{
    chargingBar->SetVisibility(ESlateVisibility::Visible);
}

void UW_UserWidget::HideBar()
{
    chargingBar->SetVisibility(ESlateVisibility::Hidden);
}

void UW_UserWidget::SetProjectileCount(ProjectileType type)
{
    switch (type)
    {
        case ProjectileType::Basic:
            basicCount++;
            break;
        case ProjectileType::Charging:
            chargingCount++;
            break;
        case ProjectileType::Split:
            splitCount++;
            break;
        case ProjectileType::Reflection:
            reflectionCount++;
            break;
        default:
            break;
    }

    FString value = FString::Printf(TEXT("basic : %d / charging : %d / split : %d / reflection : %d"), basicCount, chargingCount, splitCount, reflectionCount);
    projectileCount->SetText(FText::FromString(value));
}

void UW_UserWidget::ResetCount()
{
    basicCount = 0;
    chargingCount = 0;
    splitCount = 0;
    reflectionCount = 0;

    SetProjectileCount(ProjectileType::None);
}