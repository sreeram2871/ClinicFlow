import { AfterViewInit, Component, ElementRef, OnDestroy, ViewChild, computed, input } from '@angular/core';
import * as THREE from 'three';

interface FloatingMesh {
  mesh: THREE.Mesh<THREE.BufferGeometry, THREE.MeshStandardMaterial>;
  baseY: number;
  floatOffset: number;
  floatAmplitude: number;
  floatSpeed: number;
  rotationSpeedX: number;
  rotationSpeedY: number;
}

@Component({
  selector: 'app-three-background',
  standalone: true,
  templateUrl: './three-background.component.html',
  styleUrl: './three-background.component.scss',
})
export class ThreeBackgroundComponent implements AfterViewInit, OnDestroy {
  readonly opacity = input(1);
  readonly canvasOpacity = computed(() => Math.min(1, Math.max(0, this.opacity())));

  @ViewChild('canvasRef', { static: true })
  private canvasRef!: ElementRef<HTMLCanvasElement>;

  private scene: THREE.Scene | null = null;
  private camera: THREE.PerspectiveCamera | null = null;
  private renderer: THREE.WebGLRenderer | null = null;

  private floatingMeshes: FloatingMesh[] = [];
  private animationFrameId: number | null = null;
  private resizeHandler: (() => void) | null = null;

  ngAfterViewInit(): void {
    const canvas = this.canvasRef.nativeElement;

    this.scene = new THREE.Scene();

    this.camera = new THREE.PerspectiveCamera(55, 1, 0.1, 100);
    this.camera.position.set(0, 0.2, 7.5);

    this.renderer = new THREE.WebGLRenderer({
      canvas,
      antialias: true,
      alpha: true,
    });
    this.renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));

    const ambientLight = new THREE.AmbientLight(0xffffff, 0.55);
    this.scene.add(ambientLight);

    const directionalLight = new THREE.DirectionalLight(0xffffff, 1.05);
    directionalLight.position.set(3, 4, 6);
    this.scene.add(directionalLight);

    this.createFloatingShapes(18);

    this.resizeHandler = () => this.updateRendererSize();
    window.addEventListener('resize', this.resizeHandler);

    this.updateRendererSize();
    this.animate();
  }

  ngOnDestroy(): void {
    if (this.animationFrameId !== null) {
      cancelAnimationFrame(this.animationFrameId);
      this.animationFrameId = null;
    }

    if (this.resizeHandler) {
      window.removeEventListener('resize', this.resizeHandler);
      this.resizeHandler = null;
    }

    for (const item of this.floatingMeshes) {
      item.mesh.geometry.dispose();
      item.mesh.material.dispose();
      this.scene?.remove(item.mesh);
    }
    this.floatingMeshes = [];

    if (this.renderer) {
      this.renderer.dispose();
      this.renderer.forceContextLoss();
      this.renderer = null;
    }

    this.camera = null;
    this.scene = null;
  }

  private createFloatingShapes(count: number): void {
    if (!this.scene) {
      return;
    }

    const palette = [0x0f6e56, 0x5dcaa5, 0x1d9e75];

    for (let index = 0; index < count; index += 1) {
      const useIcosahedron = Math.random() > 0.5;
      const size = THREE.MathUtils.randFloat(0.3, 0.8);

      const geometry = useIcosahedron
        ? new THREE.IcosahedronGeometry(size, 0)
        : new THREE.TorusGeometry(size * 0.8, size * 0.22, 16, 48);

      const material = new THREE.MeshStandardMaterial({
        color: palette[Math.floor(Math.random() * palette.length)],
        roughness: 0.35,
        metalness: 0.18,
      });

      const mesh = new THREE.Mesh(geometry, material);
      mesh.position.set(
        THREE.MathUtils.randFloatSpread(10),
        THREE.MathUtils.randFloatSpread(5.5),
        THREE.MathUtils.randFloat(-6.5, 2.5),
      );

      mesh.rotation.set(
        THREE.MathUtils.randFloat(0, Math.PI),
        THREE.MathUtils.randFloat(0, Math.PI),
        THREE.MathUtils.randFloat(0, Math.PI),
      );

      this.scene.add(mesh);

      this.floatingMeshes.push({
        mesh,
        baseY: mesh.position.y,
        floatOffset: THREE.MathUtils.randFloat(0, Math.PI * 2),
        floatAmplitude: THREE.MathUtils.randFloat(0.05, 0.2),
        floatSpeed: THREE.MathUtils.randFloat(0.2, 0.45),
        rotationSpeedX: THREE.MathUtils.randFloat(0.0015, 0.004),
        rotationSpeedY: THREE.MathUtils.randFloat(0.0015, 0.005),
      });
    }
  }

  private updateRendererSize(): void {
    const canvas = this.canvasRef.nativeElement;
    const parent = canvas.parentElement;

    if (!this.renderer || !this.camera || !parent) {
      return;
    }

    const { width, height } = parent.getBoundingClientRect();
    if (width === 0 || height === 0) {
      return;
    }

    this.renderer.setSize(width, height, false);
    this.camera.aspect = width / height;
    this.camera.updateProjectionMatrix();
  }

  private animate = (): void => {
    if (!this.scene || !this.camera || !this.renderer) {
      return;
    }

    const elapsedSeconds = performance.now() * 0.001;

    for (const item of this.floatingMeshes) {
      item.mesh.rotation.x += item.rotationSpeedX;
      item.mesh.rotation.y += item.rotationSpeedY;
      item.mesh.position.y = item.baseY + Math.sin(elapsedSeconds * item.floatSpeed + item.floatOffset) * item.floatAmplitude;
    }

    this.renderer.render(this.scene, this.camera);
    this.animationFrameId = requestAnimationFrame(this.animate);
  };
}
