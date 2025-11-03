// Fill out your copyright notice in the Description page of Project Settings.


#include "AC_Projectile.h"

// Sets default values
AAC_Projectile::AAC_Projectile()
{
 	// Set this actor to call Tick() every frame.  You can turn this off to improve performance if you don't need it.
	PrimaryActorTick.bCanEverTick = true;

	// 콜리전 등록
	collisionComponent = CreateDefaultSubobject<USphereComponent>(TEXT("SphereComponent"));
	collisionComponent->InitSphereRadius(15.0f);
	RootComponent = collisionComponent;

	moveDelay = 0;
	moveSpeed = 100.0f;
	init = false;
	moveDirection = 0;
	destroyTime = 0;
	createSplit = false;
}

// Called when the game starts or when spawned
void AAC_Projectile::BeginPlay()
{
	Super::BeginPlay();
}

// Called every frame
void AAC_Projectile::Tick(float DeltaTime)
{
	Super::Tick(DeltaTime);

	if (init) 
	{
		FVector Location = GetActorLocation();
		Location += GetActorForwardVector() * moveSpeed * DeltaTime * 5.0f;
		SetActorLocation(Location);
		// 각도별 속도 조절때문에..
		//SetActorLocation(GetActorLocation() + FVector(moveDirection * moveSpeed * DeltaTime * 3, 0, 0));

		destroyTime += DeltaTime;
		if (destroyTime >= 3.0f)
		{
			ProjectileDestroy();
		}
	}
}

void AAC_Projectile::Init(float direction, FRotator rotator, bool split)
{
	moveDirection = direction;
	createSplit = split;
	init = true;
}

void AAC_Projectile::NotifyHit(UPrimitiveComponent* mySphere, AActor* other, UPrimitiveComponent* otherComponent, bool bSelfMoved, FVector hitLocation, FVector hitNormal, FVector normalImpulse, const FHitResult &Hit)
{
	/*if (GEngine)
	{
		GEngine->AddOnScreenDebugMessage(-1, 5.0f, FColor::Red, TEXT("Notify HIT"));
	}*/
}

void AAC_Projectile::NotifyActorBeginOverlap(AActor* OtherActor)
{ 
	ProjectileDestroy();
} 

void AAC_Projectile::NotifyActorEndOverlap(AActor* OtherActor)
{ 
}

void AAC_Projectile::ProjectileDestroy()
{
	Destroy();
}