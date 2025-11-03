// Fill out your copyright notice in the Description page of Project Settings.


#include "AC_ProjectileSplit.h"
#include <work/AC_Projectile.h>

// Sets default values
AAC_ProjectileSplit::AAC_ProjectileSplit()
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
void AAC_ProjectileSplit::BeginPlay()
{
	Super::BeginPlay();
	
}

// Called every frame
void AAC_ProjectileSplit::Tick(float DeltaTime)
{
	Super::Tick(DeltaTime);

	if (init)
	{
		FVector Location = GetActorLocation();
		Location += GetActorForwardVector() * moveSpeed * DeltaTime * 3.0f;
		SetActorLocation(Location);

		destroyTime += DeltaTime;
		if (destroyTime >= 3.0f)
		{
			ProjectileDestroy();
		}
	}
}

void AAC_ProjectileSplit::Init(float direction, FRotator rotator)
{
	moveDirection = direction;
	init = true;
}

void AAC_ProjectileSplit::NotifyActorBeginOverlap(AActor* OtherActor)
{
	Destroy();
}

void AAC_ProjectileSplit::NotifyActorEndOverlap(AActor* OtherActor)
{
}

void AAC_ProjectileSplit::ProjectileDestroy()
{
	if (projectileClass)
	{
		float direction = GetActorForwardVector().X;
		//FVector::ForwardVector;
		FVector vector = GetActorLocation();
		FRotator rotation = GetActorRotation();
		UWorld* world = GetWorld();

		AAC_Projectile* projectile = world->SpawnActor<AAC_Projectile>(projectileClass, vector, rotation + FRotator(45.0f, 0, 0));
		projectile->Init(direction, rotation, true);

		AAC_Projectile* projectile2 = world->SpawnActor<AAC_Projectile>(projectileClass, vector, rotation + FRotator(-45.0f, 0, 0));
		projectile2->Init(direction, rotation, true);

		Destroy();
	}
}

