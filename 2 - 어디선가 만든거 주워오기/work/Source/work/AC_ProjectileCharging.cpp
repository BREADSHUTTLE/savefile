// Fill out your copyright notice in the Description page of Project Settings.


#include "AC_ProjectileCharging.h"

// Sets default values
AAC_ProjectileCharging::AAC_ProjectileCharging()
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
void AAC_ProjectileCharging::BeginPlay()
{
	Super::BeginPlay();
	
}

// Called every frame
void AAC_ProjectileCharging::Tick(float DeltaTime)
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

void AAC_ProjectileCharging::Init(float direction, FRotator rotator)
{
	moveDirection = direction;
	init = true;
}

void AAC_ProjectileCharging::NotifyActorBeginOverlap(AActor* OtherActor)
{
	ProjectileDestroy();
}

void AAC_ProjectileCharging::NotifyActorEndOverlap(AActor* OtherActor)
{
}

void AAC_ProjectileCharging::ProjectileDestroy()
{
	Destroy();
}

