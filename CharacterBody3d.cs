using Godot;
using System;

public partial class CharacterBody3d : CharacterBody3D
{
	// Скорость движения персонажа
	public const float Speed = 5.0f;

	// Скорость бега персонажа
	public const float RunSpeed = 10.0f;

	// Сила прыжка
	public const float JumpVelocity = 4.5f;

	// Гравитация
	public const float Gravity = -9.8f;

	// Чувствительность мыши
	public const float MouseSensitivity = 0.1f;

	// Высота при приседании
	private const float CrouchHeight = 0.5f;

	// Скорость плавного перемещения камеры
	private const float CameraMoveSpeed = 5.0f;

	// Указатель на камеру
	private Camera3D _camera;

	// Указатель на CollisionShape3D
	private CollisionShape3D _collisionShape;

	// Указатель на MeshInstance3D (модель персонажа)
	private MeshInstance3D _meshInstance;

	// Переменная для отслеживания состояния приседания
	private bool _isCrouching = false;

	// Оригинальная высота капсулы
	private float _defaultCollisionHeight;

	// Оригинальный масштаб модели
	private Vector3 _defaultScale;

	public override void _Ready()
	{
		// Инициализируем камеру
		_camera = GetNode<Camera3D>("Camera3D");

		// Инициализируем коллайдер
		_collisionShape = GetNode<CollisionShape3D>("CollisionShape3D");

		// Инициализируем MeshInstance3D
		_meshInstance = GetNode<MeshInstance3D>("MeshInstance3D");

		// Сохраняем оригинальную высоту капсулы
		if (_collisionShape.Shape is CapsuleShape3D capsule)
		{
			_defaultCollisionHeight = capsule.Height;
		}

		// Сохраняем оригинальный масштаб модели
		_defaultScale = _meshInstance.Scale;

		// Скрываем курсор и фиксируем его положение
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;

		// Добавляем гравитацию
		if (!IsOnFloor())
		{
			velocity.Y += Gravity * (float)delta;
		}

		// Обработка прыжка
		if (Input.IsActionJustPressed("ui_accept") && IsOnFloor() && !_isCrouching)
		{
			velocity.Y = JumpVelocity;
		}

		// Направление движения (WASD)
		Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
		Vector3 direction = (Transform.Basis.X * inputDir.X + Transform.Basis.Z * inputDir.Y).Normalized();

		// Определяем текущую скорость (бег или ходьба)
		float currentSpeed = Input.IsActionPressed("run") && !_isCrouching ? RunSpeed : Speed;

		if (direction != Vector3.Zero)
		{
			// Применяем скорость движения в локальных координатах
			velocity.X = direction.X * currentSpeed;
			velocity.Z = direction.Z * currentSpeed;
		}
		else
		{
			// Плавное замедление
			velocity.X = Mathf.MoveToward(velocity.X, 0, Speed * (float)delta);
			velocity.Z = Mathf.MoveToward(velocity.Z, 0, Speed * (float)delta);
		}

		// Применяем обновлённую скорость
		Velocity = velocity;

		// Двигаем персонажа и обрабатываем столкновения
		MoveAndSlide();

		// Обработка приседания
		if (Input.IsActionJustPressed("crouch"))
		{
			_isCrouching = !_isCrouching;

			if (_isCrouching)
			{
				// Уменьшаем высоту коллайдера
				if (_collisionShape.Shape is CapsuleShape3D capsule)
				{
					capsule.Height = _defaultCollisionHeight * CrouchHeight;
				}

				// Масштабируем модель для эффекта приседания
				_meshInstance.Scale = new Vector3(_defaultScale.X, _defaultScale.Y * CrouchHeight, _defaultScale.Z);
			}
			else
			{
				// Возвращаем высоту коллайдера
				if (_collisionShape.Shape is CapsuleShape3D capsule)
				{
					capsule.Height = _defaultCollisionHeight;
				}

				// Восстанавливаем масштаб модели
				_meshInstance.Scale = _defaultScale;
			}
		}

		// Плавно изменяем локальную позицию камеры относительно персонажа
		Vector3 targetCameraOffset = _isCrouching 
			? new Vector3(0, _defaultCollisionHeight * CrouchHeight / 2, 0)
			: new Vector3(0, _defaultCollisionHeight / 2, 0);

		_camera.Position = _camera.Position.Lerp(targetCameraOffset, (float)(CameraMoveSpeed * delta));
	}

	public override void _Input(InputEvent @event)
	{
		// Управление камерой мышью
		if (@event is InputEventMouseMotion mouseMotion)
		{
			Vector3 rotation = RotationDegrees;

			// Изменяем угол поворота персонажа по горизонтали
			rotation.Y -= mouseMotion.Relative.X * MouseSensitivity;

			// Изменяем угол наклона камеры по вертикали
			Vector3 cameraRotation = _camera.RotationDegrees;
			cameraRotation.X -= mouseMotion.Relative.Y * MouseSensitivity;
			cameraRotation.X = Mathf.Clamp(cameraRotation.X, -90, 90);

			RotationDegrees = rotation;
			_camera.RotationDegrees = cameraRotation;
		}
	}
}
