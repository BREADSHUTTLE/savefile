// Copyright Epic Games, Inc. All Rights Reserved.

#include "workCharacter.h"
#include "Camera/CameraComponent.h"
#include "Components/CapsuleComponent.h"
#include "Components/InputComponent.h"
#include "GameFramework/SpringArmComponent.h"
#include "GameFramework/CharacterMovementComponent.h"
#include <work/AC_Projectile.h>
#include <work/AC_ProjectileCharging.h>
#include <work/AC_ProjectileSplit.h>
#include <work/AC_ProjectileReflection.h>
#include <work/W_UserWidget.h>

AworkCharacter::AworkCharacter()
{
	// Set size for collision capsule
	GetCapsuleComponent()->InitCapsuleSize(42.f, 96.0f);

	// Don't rotate when the controller rotates.
	bUseControllerRotationPitch = false;
	bUseControllerRotationYaw = false;
	bUseControllerRotationRoll = false;

	// Create a camera boom attached to the root (capsule)
	CameraBoom = CreateDefaultSubobject<USpringArmComponent>(TEXT("CameraBoom"));
	CameraBoom->SetupAttachment(RootComponent);
	CameraBoom->SetUsingAbsoluteRotation(true); // Rotation of the character should not affect rotation of boom
	CameraBoom->bDoCollisionTest = false;
	CameraBoom->TargetArmLength = 500.f;
	CameraBoom->SocketOffset = FVector(0.f,0.f,75.f);
	CameraBoom->SetRelativeRotation(FRotator(0.f,180.f,0.f));

	// Create a camera and attach to boom
	SideViewCameraComponent = CreateDefaultSubobject<UCameraComponent>(TEXT("SideViewCamera"));
	SideViewCameraComponent->SetupAttachment(CameraBoom, USpringArmComponent::SocketName);
	SideViewCameraComponent->bUsePawnControlRotation = false; // We don't want the controller rotating the camera

	// Configure character movement
	GetCharacterMovement()->bOrientRotationToMovement = true; // Face in the direction we are moving..
	GetCharacterMovement()->RotationRate = FRotator(0.0f, 720.0f, 0.0f); // ...at this rotation rate
	GetCharacterMovement()->GravityScale = 2.f;
	GetCharacterMovement()->AirControl = 0.80f;
	GetCharacterMovement()->JumpZVelocity = 1000.f;
	GetCharacterMovement()->GroundFriction = 3.f;
	GetCharacterMovement()->MaxWalkSpeed = 600.f;
	GetCharacterMovement()->MaxFlySpeed = 600.f;

	// Note: The skeletal mesh and anim blueprint references on the Mesh component (inherited from Character) 
	// are set in the derived blueprint asset named MyCharacter (to avoid direct content references in C++)

	split = false;
	charging = false;
	maxCharging = 3.0f;
	clickTime = 0;

	curState = State::Ready;
}

//////////////////////////////////////////////////////////////////////////
// Input

//AAC_Projectile* projectile;

void AworkCharacter::BeginPlay()
{
	Super::BeginPlay();

	GameWidget();
}

void AworkCharacter::GameWidget()
{
	if (widgetMainClass != nullptr)
	{
		UWorld* world = GetWorld();
		widgetMain = CreateWidget<UW_UserWidget>(world, widgetMainClass);

		if (widgetMain != nullptr)
		{
			widgetMain->AddToViewport();
			widgetMain->HideBar();
			widgetMain->SetProjectileCount(UW_UserWidget::ProjectileType::None);
		}
	}
}

void AworkCharacter::Tick(float DeltaTime)
{
	Super::Tick(DeltaTime);

	if (charging)
	//if (charging && split == false)
	{
		clickTime += DeltaTime;
		if (clickTime > 0.5f) 
		{
			widgetMain->SetChargeTime(clickTime, maxCharging);
			widgetMain->ShowBar();
		}
	}
}

void AworkCharacter::SetupPlayerInputComponent(class UInputComponent* PlayerInputComponent)
{
	// set up gameplay key bindings
	//PlayerInputComponent->BindAction("Jump", IE_Pressed, this, &ACharacter::Jump);
	//PlayerInputComponent->BindAction("Jump", IE_Released, this, &ACharacter::StopJumping);
	PlayerInputComponent->BindAxis("MoveRight", this, &AworkCharacter::MoveRight);

	PlayerInputComponent->BindTouch(IE_Pressed, this, &AworkCharacter::TouchStarted);
	PlayerInputComponent->BindTouch(IE_Released, this, &AworkCharacter::TouchStopped);

	PlayerInputComponent->BindAction("Projectile", IE_Pressed, this, &AworkCharacter::ClickTimeCheck);
	PlayerInputComponent->BindAction("Projectile", IE_Released, this, &AworkCharacter::Shoot);

	PlayerInputComponent->BindAction("Projectile_w", IE_Pressed, this, &AworkCharacter::ShootSplit);
	PlayerInputComponent->BindAction("Projectile_w", IE_Released, this, &AworkCharacter::ShootReflection);
}

void AworkCharacter::ClickTimeCheck()
{
	clickTime = 0;
	charging = true;
	split = false;

	//reflection = false;

	curState = State::Ready;
}

void AworkCharacter::Shoot()
{
	charging = false;

	if (clickTime >= maxCharging)
		curState = State::Charging;
	else 
		curState = State::Basic;

	if (split == false)
	{
		BaseProjectile();
	}
}

void AworkCharacter::ShootSplit()
{
	if (clickTime <= 1.0f)
	{
		clickTime = 0;
		charging = false;
		split = true;

		curState = State::Split;

		//if (charging) 
		{
			BaseProjectile();
		}
	}
}

void AworkCharacter::ShootReflection()
{
	//split = false;
	//reflection = true;
	if (curState != State::Split)
	//if (charging == false) 
	{
		curState = State::Reflection;
		BaseProjectile();
	}
}

void AworkCharacter::BaseProjectile()
{
	float direction = GetActorForwardVector().X;
	if (direction >= 1.0f || direction <= -0.9f)
	{
		float vectorX = direction >= 1.0f ? 20.0f : -20.0f;
		FVector vector = GetActorLocation() + FVector(vectorX, 0, 50.0f);
		FRotator rotation = GetActorRotation();

		UWorld* world = GetWorld();
		if (world)
		{
			FActorSpawnParameters spawnParams;
			spawnParams.Owner = this;
			
			if (curState == State::Split)
			//if (split) 
			{
				if (projectileSplitClass)
				{
					AAC_ProjectileSplit* projectileSplit = world->SpawnActor<AAC_ProjectileSplit>(projectileSplitClass, vector, rotation);
					projectileSplit->Init(direction, rotation);

					widgetMain->SetProjectileCount(UW_UserWidget::ProjectileType::Split);
				}
			}
			else if (curState == State::Reflection)
			//else if (reflection)
			{
				if (projectileReflectionClass)
				{
					AAC_ProjectileReflection* projectileReflection = world->SpawnActor<AAC_ProjectileReflection>(projectileReflectionClass, vector, rotation);
					projectileReflection->Init(direction, rotation);

					widgetMain->SetProjectileCount(UW_UserWidget::ProjectileType::Reflection);
				}
			}
			else
			{
				if (curState == State::Charging)
				//if (clickTime >= maxCharging)
				{
					if (projectileChargingClass)
					{
						AAC_ProjectileCharging* projectileCharging = world->SpawnActor<AAC_ProjectileCharging>(projectileChargingClass, vector, rotation);
						projectileCharging->Init(direction, rotation);

						widgetMain->SetProjectileCount(UW_UserWidget::ProjectileType::Charging);
					}
				}
				else if (curState == State::Basic)
				//else
				{
					if (projectileClass)
					{
						AAC_Projectile* projectile = world->SpawnActor<AAC_Projectile>(projectileClass, vector, rotation);
						projectile->Init(direction, rotation, false);

						widgetMain->SetProjectileCount(UW_UserWidget::ProjectileType::Basic);
					}
				}
			}

			widgetMain->HideBar();
			//curState = State::Ready;
		}

	}
}

void AworkCharacter::MoveRight(float Value)
{
	// add movement in that direction
	AddMovementInput(FVector(-1.f, 0.f,0.f), Value);
}

void AworkCharacter::TouchStarted(const ETouchIndex::Type FingerIndex, const FVector Location)
{
	// jump on any touch
	//Jump();
}

void AworkCharacter::TouchStopped(const ETouchIndex::Type FingerIndex, const FVector Location)
{
	//StopJumping();
}

