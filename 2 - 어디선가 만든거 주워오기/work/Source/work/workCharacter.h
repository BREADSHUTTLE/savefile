// Copyright Epic Games, Inc. All Rights Reserved.

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Character.h"
#include "workCharacter.generated.h"

UCLASS(config=Game)
class AworkCharacter : public ACharacter
{
	GENERATED_BODY()

	/** Side view camera */
	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = Camera, meta = (AllowPrivateAccess = "true"))
	class UCameraComponent* SideViewCameraComponent;

	/** Camera boom positioning the camera beside the character */
	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category = Camera, meta = (AllowPrivateAccess = "true"))
	class USpringArmComponent* CameraBoom;

private:
	enum State
	{
		Ready,
		Basic,
		Charging,
		Split,
		Reflection,
	};

	State curState;

	bool charging;
	float maxCharging;
	float clickTime;
	bool split;
	bool reflection;

protected:

	virtual void BeginPlay() override;	// 스타트

	/** Called for side to side input */
	void MoveRight(float Val);

	/** Handle touch inputs. */
	void TouchStarted(const ETouchIndex::Type FingerIndex, const FVector Location);

	/** Handle touch stop event. */
	void TouchStopped(const ETouchIndex::Type FingerIndex, const FVector Location);

	// APawn interface
	virtual void SetupPlayerInputComponent(class UInputComponent* InputComponent) override;
	// End of APawn interface

	void ClickTimeCheck();

	void Shoot();

	void ShootSplit();

	void ShootReflection();

	// 기본
	void BaseProjectile();

	//void ChargingProjectile();

	void GameWidget();

public:

	AworkCharacter();

	virtual void Tick(float DeltaTime) override;	// 업데이트

	/** Returns SideViewCameraComponent subobject **/
	FORCEINLINE class UCameraComponent* GetSideViewCameraComponent() const { return SideViewCameraComponent; }
	/** Returns CameraBoom subobject **/
	FORCEINLINE class USpringArmComponent* GetCameraBoom() const { return CameraBoom; }

	UPROPERTY(EditDefaultsOnly, Category = Projectile)
	TSubclassOf<class AAC_Projectile> projectileClass;

	UPROPERTY(EditDefaultsOnly, Category = Projectile)
	TSubclassOf<class AAC_ProjectileCharging> projectileChargingClass;

	UPROPERTY(EditDefaultsOnly, Category = Projectile)
	TSubclassOf<class AAC_ProjectileSplit> projectileSplitClass;

	UPROPERTY(EditDefaultsOnly, Category = Projectile)
	TSubclassOf<class AAC_ProjectileReflection> projectileReflectionClass;

	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category = "Widget", Meta = (AllowPrivateAccess = true))
	TSubclassOf<class UW_UserWidget> widgetMainClass;

	class UW_UserWidget* widgetMain;
};
