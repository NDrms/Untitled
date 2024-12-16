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

	// Указатель на камеру
	private Camera3D _camera;

	// Переменная для отслеживания состояния приседания
	private bool _isCrouching = false;

	public override void _Ready()
	{
		// Инициализируем камеру (замените "Camera3D" на имя узла вашей камеры)
		_camera = GetNode<Camera3D>("Camera3D");

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
		if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
		{
			velocity.Y = JumpVelocity;
		}

		// Направление движения (WASD)
		Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
		Vector3 direction = (Transform.Basis.X * inputDir.X + Transform.Basis.Z * inputDir.Y).Normalized();

		// Определяем текущую скорость (бег или ходьба)
		float currentSpeed = Input.IsActionPressed("run") ? RunSpeed : Speed;

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
			// Измените высоту персонажа или другие параметры для приседания, если необходимо
			if (_isCrouching)
			{
				// Пример уменьшения высоты персонажа
				// Можно адаптировать под вашу модель или логику
				Scale = new Vector3(1, 0.5f, 1);
			}
			else
			{
				// Возвращаемся в обычное положение
				Scale = new Vector3(1, 1, 1);
			}
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
