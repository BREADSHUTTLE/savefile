// Fill out your copyright notice in the Description page of Project Settings.


#include "AC_ProjectileReflection.h"

// Sets default values
AAC_ProjectileReflection::AAC_ProjectileReflection()
{
 	// Set this actor to call Tick() every frame.  You can turn this off to improve performance if you don't need it.
	PrimaryActorTick.bCanEverTick = true;

	collisionComponent = CreateDefaultSubobject<USphereComponent>(TEXT("SphereComponent"));
	collisionComponent->InitSphereRadius(15.0f);
	RootComponent = collisionComponent;

	moveDirection = 0;
	destroyTime = 0;
	moveSpeed = 100.0f;
	init = false;
}

// Called when the game starts or when spawned
void AAC_ProjectileReflection::BeginPlay()
{
	Super::BeginPlay();
	
}

// Called every frame
void AAC_ProjectileReflection::Tick(float DeltaTime)
{
	Super::Tick(DeltaTime);

	if (init)
	{
		FVector Location = GetActorLocation();
		Location += GetActorForwardVector() * moveSpeed * DeltaTime * 5.0f;
		SetActorLocation(Location);

		destroyTime += DeltaTime;
		if (destroyTime >= 5.0f)
		{
			ProjectileDestroy();
		}
	}
}

void AAC_ProjectileReflection::Init(float direction, FRotator rotator)
{
	moveDirection = direction;
	init = true;
}

void AAC_ProjectileReflection::NotifyActorBeginOverlap(AActor* OtherActor)
{
	Reflection();
}

void AAC_ProjectileReflection::NotifyActorEndOverlap(AActor* OtherActor)
{
}

void AAC_ProjectileReflection::ProjectileDestroy()
{
	Destroy();
}

void AAC_ProjectileReflection::Reflection()
{
	/*if (GEngine)
		GEngine->AddOnScreenDebugMessage(-1, 5.0f, FColor::Blue, TEXT("Reflection"));*/
		 
	// 액터 회전시키기
	float direction = GetActorForwardVector().X;

	FRotator rotation = GetActorRotation() + FRotator(direction * 180.0f, 0, 0);
	SetActorRotation(rotation);
}
