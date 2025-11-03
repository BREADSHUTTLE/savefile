// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "Blueprint/UserWidget.h"
#include <Runtime/UMG/Public/Components/ProgressBar.h>
#include <Runtime/UMG/Public/Components/Button.h>
#include <Runtime/UMG/Public/Components/TextBlock.h>
#include "W_UserWidget.generated.h"

/**
 * 
 */

UCLASS()
class WORK_API UW_UserWidget : public UUserWidget
{
	GENERATED_BODY()
	

protected:

	virtual void NativeOnInitialized();

	UPROPERTY(meta = (BindWidget))
		UProgressBar* chargingBar;

	UPROPERTY(meta = (BindWidget))
		UButton* resetButton;

	UPROPERTY(meta = (BindWidget))
		UTextBlock* projectileCount;

private:
	int basicCount, chargingCount, splitCount, reflectionCount = 0;

public:
	enum ProjectileType
	{
		None,
		Basic,
		Charging,
		Split,
		Reflection,
	};

	void SetChargeTime(float value, float maxValue);
	void ShowBar();
	void HideBar();

	void SetProjectileCount(ProjectileType type);

	UFUNCTION(BlueprintCallable)	// 해당 함수를 블루프린트에서도 호출 가능하게 해줌
	void ResetCount();
};
