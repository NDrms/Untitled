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

	// Скорость плавного перемещения камеры и коллайдера
	private const float SmoothMoveSpeed = 5.0f;

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

	// Текущая высота коллайдера
	private float _currentCollisionHeight;

	// Указатель на узел оружия
	private Node3D _weapon;

	public override void _Ready()
	{
		// Инициализация камеры
		_camera = GetNode<Camera3D>("Camera3D");

		// Инициализация оружия
		_weapon = GetNode<Node3D>("Node/Weapon"); // Убедитесь, что узел оружия называется "Weapon"

		// Инициализация других узлов
		_collisionShape = GetNode<CollisionShape3D>("CollisionShape3D");
		_meshInstance = GetNode<MeshInstance3D>("MeshInstance3D");

		// Сохраняем оригинальную высоту капсулы
		if (_collisionShape.Shape is CapsuleShape3D capsule)
		{
			_defaultCollisionHeight = capsule.Height;
			_currentCollisionHeight = _defaultCollisionHeight;
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
		}

		// Плавно изменяем локальную позицию камеры относительно персонажа
		Vector3 targetCameraOffset = _isCrouching 
			? new Vector3(0, _defaultCollisionHeight * CrouchHeight / 2, 0)
			: new Vector3(0, _defaultCollisionHeight / 2, 0);

		_camera.Position = _camera.Position.Lerp(targetCameraOffset, (float)(SmoothMoveSpeed * delta));

		// Плавно изменяем высоту коллайдера
		float targetHeight = _isCrouching ? _defaultCollisionHeight * CrouchHeight : _defaultCollisionHeight;
		_currentCollisionHeight = Mathf.Lerp(_currentCollisionHeight, targetHeight, (float)(SmoothMoveSpeed * delta));

		// Применяем плавно изменяющуюся высоту коллайдера
		if (_collisionShape.Shape is CapsuleShape3D capsule)
		{
			capsule.Height = _currentCollisionHeight;
		}

		// Плавно изменяем масштаб модели
		_meshInstance.Scale = _meshInstance.Scale.Lerp(
			_isCrouching ? new Vector3(_defaultScale.X, _defaultScale.Y * CrouchHeight, _defaultScale.Z) : _defaultScale,
			(float)(SmoothMoveSpeed * delta));

		// Обновление позиции и ориентации оружия
		if (_weapon != null)
		{
			// Позиционируем оружие относительно камеры с небольшим отступом
			Vector3 weaponOffset = new Vector3(0.5f, -0.3f, 0);  // Можно настроить отступ по своему усмотрению
			_weapon.Position = _camera.Position + _camera.Transform.Basis.Z * -0.5f + _camera.Transform.Basis.Y * 0.3f + weaponOffset;

			// Ориентируем оружие в том направлении, в котором смотрит камера
			_weapon.LookAt(_camera.Position + _camera.Transform.Basis.Z * 10.0f);  // Оружие будет всегда смотреть в направлении камеры
		}
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
