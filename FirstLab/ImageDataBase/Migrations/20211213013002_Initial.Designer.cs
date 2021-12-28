﻿// <auto-generated />
using System;
using ImageDataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ImageDataBase.Migrations
{
    [DbContext(typeof(ImageDB))]
    [Migration("20211213013002_Initial")]
    partial class Initial
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "5.0.12");

            modelBuilder.Entity("ImageDataBase.Image", b =>
                {
                    b.Property<int>("ImageId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("ImageHash")
                        .HasColumnType("TEXT");

                    b.Property<string>("ImageName")
                        .HasColumnType("TEXT");

                    b.Property<int?>("ImagePhotoImageDataId")
                        .HasColumnType("INTEGER");

                    b.HasKey("ImageId");

                    b.HasIndex("ImagePhotoImageDataId");

                    b.ToTable("Images");
                });

            modelBuilder.Entity("ImageDataBase.ImageData", b =>
                {
                    b.Property<int>("ImageDataId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<byte[]>("ImageDataArray")
                        .HasColumnType("BLOB");

                    b.HasKey("ImageDataId");

                    b.ToTable("ImageData");
                });

            modelBuilder.Entity("ImageDataBase.ImageObject", b =>
                {
                    b.Property<int>("ImageObjectId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int?>("ImageId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ImageObjectName")
                        .HasColumnType("TEXT");

                    b.Property<float>("X1")
                        .HasColumnType("REAL");

                    b.Property<float>("X2")
                        .HasColumnType("REAL");

                    b.Property<float>("Y1")
                        .HasColumnType("REAL");

                    b.Property<float>("Y2")
                        .HasColumnType("REAL");

                    b.HasKey("ImageObjectId");

                    b.HasIndex("ImageId");

                    b.ToTable("ImageObject");
                });

            modelBuilder.Entity("ImageDataBase.Image", b =>
                {
                    b.HasOne("ImageDataBase.ImageData", "ImagePhoto")
                        .WithMany()
                        .HasForeignKey("ImagePhotoImageDataId");

                    b.Navigation("ImagePhoto");
                });

            modelBuilder.Entity("ImageDataBase.ImageObject", b =>
                {
                    b.HasOne("ImageDataBase.Image", null)
                        .WithMany("ImageObjects")
                        .HasForeignKey("ImageId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("ImageDataBase.Image", b =>
                {
                    b.Navigation("ImageObjects");
                });
#pragma warning restore 612, 618
        }
    }
}
