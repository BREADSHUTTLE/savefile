// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "Engine/Classes/Components/SphereComponent.h"
#include "AC_ProjectileSplit.generated.h"

UCLASS()
class WORK_API AAC_ProjectileSplit : public AActor
{
	GENERATED_BODY()
	
public:	
	// Sets default values for this actor's properties
	AAC_ProjectileSplit();

private:

	float moveDirection;
	float destroyTime;
	float moveSpeed;
	bool init;

protected:
	// Called when the game starts or when spawned
	virtual void BeginPlay() override;

	virtual void NotifyActorBeginOverlap(AActor* OtherActor) override;
	virtual void NotifyActorEndOverlap(AActor* OtherActor) override;

public:	
	// Called every frame
	virtual void Tick(float DeltaTime) override;

	UPROPERTY(VisibleDefaultsOnly, Category = Projectile)
		USphereComponent* collisionComponent;

	void ProjectileDestroy();

	void Init(float direction, FRotator rotator);

	UPROPERTY(EditDefaultsOnly, Category = Projectile)
	TSubclassOf<class AAC_Projectile> projectileClass;
};
